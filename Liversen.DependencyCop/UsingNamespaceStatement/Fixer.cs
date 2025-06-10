using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Liversen.DependencyCop.UsingNamespaceStatement
{
    class Fixer
    {
        readonly Document document;
        readonly UsingDirectiveSyntax usingDirective;
        readonly DottedName usingDirectiveName;
        readonly SemanticModel semanticModel;
        readonly DocumentEditor editor;
        readonly StaticUsingsSet staticUsingDirectives;

        Fixer(
            Document document,
            UsingDirectiveSyntax usingDirective,
            DottedName usingDirectiveName,
            SemanticModel semanticModel,
            DocumentEditor editor,
            StaticUsingsSet staticUsingDirectives)
        {
            this.document = document;
            this.usingDirective = usingDirective;
            this.usingDirectiveName = usingDirectiveName;
            this.semanticModel = semanticModel;
            this.editor = editor;
            this.staticUsingDirectives = staticUsingDirectives;
        }

        public static async Task<Document> Fix(Document document, UsingDirectiveSyntax usingDirective, CancellationToken cancellationToken)
        {
            if (usingDirective.Name == null)
            {
                return document;
            }

            var usingDirectiveName = new DottedName(usingDirective.Name.ToString());

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            if (semanticModel == null)
            {
                return document;
            }

            var editor = await DocumentEditor.CreateAsync(document, cancellationToken);
            var staticUsingDirecetive = await StaticUsingsSet.GetExisingStaticUsings(document, cancellationToken);

            var fixer = new Fixer(document, usingDirective, usingDirectiveName, semanticModel, editor, staticUsingDirecetive);

            return await fixer.Fix(cancellationToken);
        }

        async Task<Document> Fix(CancellationToken cancellationToken)
        {
            var violations = await FindViolations(cancellationToken);

            FixViolations(violations, cancellationToken);

            editor.RemoveNode(usingDirective);

            return editor.GetChangedDocument();
        }

        async Task<IEnumerable<Violation>> FindViolations(CancellationToken cancellationToken)
        {
            var rootNode = await document.GetSyntaxRootAsync(cancellationToken);
            if (rootNode == null)
            {
                return Enumerable.Empty<Violation>();
            }

            var violations = new List<Violation>();
            var classDeclarations = rootNode.DescendantNodes().OfType<ClassDeclarationSyntax>();
            foreach (var classDeclaration in classDeclarations)
            {
                var declaredSymbol = semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken);
                if (declaredSymbol == null)
                {
                    continue;
                }

                var typeOuterNamespace = Helpers.ContainingNamespace(declaredSymbol);
                if (typeOuterNamespace == null || typeOuterNamespace == usingDirectiveName)
                {
                    continue;
                }

                var typeDeclarations = classDeclaration.DescendantNodes().OfType<SimpleNameSyntax>();

                violations.AddRange(FilterTypeDeclarationWithinSpecifiedNamespace(typeDeclarations, typeOuterNamespace, cancellationToken));
            }

            return violations;
        }

        IEnumerable<Violation> FilterTypeDeclarationWithinSpecifiedNamespace(IEnumerable<SimpleNameSyntax> typeDeclarations, DottedName typeOuterNamespace, CancellationToken cancellationToken)
        {
            if (typeOuterNamespace != usingDirectiveName)
            {
                foreach (var typeDecl in typeDeclarations)
                {
                    var symbol = semanticModel.GetSymbolInfo(typeDecl, cancellationToken).Symbol;
                    var symbolContainingNamespace = Helpers.ContainingNamespace(symbol);
                    if (symbolContainingNamespace == usingDirectiveName)
                    {
                        yield return new Violation(typeOuterNamespace, typeDecl);
                    }
                }
            }
        }

        void FixViolations(IEnumerable<Violation> violations, CancellationToken cancellationToken)
        {
            foreach (var violation in violations)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(violation.ViolatingNode, cancellationToken);
                var symbol = symbolInfo.Symbol;
                if (symbol?.ContainingNamespace != null &&
                    symbol.ContainingNamespace.ToDisplayString() == usingDirectiveName.Value)
                {
                    var fullNameSpace = symbol.ToDisplayString();

                    var possibleMethodCall = violation.ViolatingNode.Parent;
                    if (possibleMethodCall is MemberAccessExpressionSyntax)
                    {
                        if (semanticModel.GetSymbolInfo(possibleMethodCall, cancellationToken).Symbol is IMethodSymbol possibleExtensionMethod
                            && possibleExtensionMethod.IsExtensionMethod)
                        {
                            FixByAddingStaticUsingDirectiveForExtensionMethodCall(symbol);
                        }
                    }
                    else
                    {
                        FixByQualifyingUsageOfType(fullNameSpace, violation);
                    }
                }
            }
        }

        void FixByQualifyingUsageOfType(string fullNameSpace, Violation violation)
        {
            var replacementName = new DottedName(fullNameSpace).SkipCommonPrefix(violation.Namespace);
            if (replacementName != null)
            {
                var qualifiedName = SyntaxFactory.ParseName(replacementName.Value)
                    .WithLeadingTrivia(violation.ViolatingNode.GetLeadingTrivia())
                    .WithTrailingTrivia(violation.ViolatingNode.GetTrailingTrivia());

                // At least some namespace already present - maybe even too much.
                if (violation.ViolatingNode.Parent is QualifiedNameSyntax identifierQualifiedNameSyntax)
                {
                    if (identifierQualifiedNameSyntax.ToFullString() != qualifiedName.ToFullString())
                    {
                        editor.ReplaceNode(identifierQualifiedNameSyntax, qualifiedName);
                    }

                    // Else do nothing - already qualified as it should be.
                }
                else
                {
                    editor.ReplaceNode(violation.ViolatingNode, qualifiedName);
                }
            }
        }

        void FixByAddingStaticUsingDirectiveForExtensionMethodCall(ISymbol symbol)
        {
            var staticUsingDirective = SyntaxFactory.UsingDirective(SyntaxFactory.Token(SyntaxKind.StaticKeyword), null, SyntaxFactory.ParseName(symbol.ContainingType.ToString()));

            if (staticUsingDirectives.Add(staticUsingDirective))
            {
                editor.InsertBefore(usingDirective, staticUsingDirective);
            }
        }

        sealed class Violation
        {
            public Violation(DottedName @namespace, TypeSyntax violatingNode)
            {
                Namespace = @namespace;
                ViolatingNode = violatingNode;
            }

            public DottedName Namespace { get; }

            public TypeSyntax ViolatingNode { get; }
        }
    }
}
