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
        readonly SemanticModel semanticModel;
        readonly DocumentEditor editor;
        readonly DottedName namespaceName;
        readonly Document document;
        readonly UsingDirectiveSyntax usingDirective;
        readonly StaticUsingsSet staticUsings;

        Fixer(SemanticModel semanticModel, DocumentEditor editor, DottedName namespaceName, Document document, UsingDirectiveSyntax usingDirective, StaticUsingsSet staticUsings)
        {
            this.semanticModel = semanticModel;
            this.editor = editor;
            this.namespaceName = namespaceName;
            this.document = document;
            this.usingDirective = usingDirective;
            this.staticUsings = staticUsings;
        }

        public static async Task<Document> ApplyFix(Document document, UsingDirectiveSyntax usingDirective, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            if (semanticModel == null)
            {
                return document;
            }

            var editor = await DocumentEditor.CreateAsync(document, cancellationToken);
            if (usingDirective.Name == null)
            {
                return document;
            }

            var namespaceName = new DottedName(usingDirective.Name.ToString());
            var staticUsings = await StaticUsingsSet.GetExisingStaticUsings(document, cancellationToken);

            var fixAction = new Fixer(semanticModel, editor, namespaceName, document, usingDirective, staticUsings);

            return await fixAction.ApplyFix(cancellationToken);
        }

        async Task<Document> ApplyFix(CancellationToken cancellationToken)
        {
            var classDeclarations = await FindNameSpaceUsages(cancellationToken);
            FixNameSpaceUsages(classDeclarations, cancellationToken);

            editor.RemoveNode(usingDirective);

            return editor.GetChangedDocument();
        }

        async Task<List<Violation>> FindNameSpaceUsages(CancellationToken cancellationToken)
        {
            var back = new List<Violation>();

            var rootNode = await document.GetSyntaxRootAsync(cancellationToken);
            if (rootNode == null)
            {
                return back;
            }

            var nameSpaceDeclarations = rootNode.DescendantNodes().OfType<ClassDeclarationSyntax>();
            foreach (var namespaceDeclarationSyntax in nameSpaceDeclarations)
            {
                var declaredSymbol = semanticModel.GetDeclaredSymbol(namespaceDeclarationSyntax, cancellationToken);
                if (declaredSymbol == null)
                {
                    continue;
                }

                var typeOuterNamespace = Helpers.ContainingNamespace(declaredSymbol);
                if (typeOuterNamespace == null || typeOuterNamespace == namespaceName)
                {
                    continue;
                }

                var typeDeclarations = namespaceDeclarationSyntax.DescendantNodes().OfType<SimpleNameSyntax>();

                back.AddRange(FilterTypeDeclarationWithinSpecifiedNamespace(typeDeclarations, typeOuterNamespace, cancellationToken));
            }

            return back;
        }

        IEnumerable<Violation> FilterTypeDeclarationWithinSpecifiedNamespace(IEnumerable<SimpleNameSyntax> typeDeclarations, DottedName typeOuterNamespace, CancellationToken cancellationToken)
        {
            if (typeOuterNamespace != namespaceName)
            {
                foreach (var typeDecl in typeDeclarations)
                {
                    var symbol = semanticModel.GetSymbolInfo(typeDecl, cancellationToken).Symbol;
                    var symbolContainingNamespace = Helpers.ContainingNamespace(symbol);
                    if (symbolContainingNamespace == namespaceName)
                    {
                        yield return new Violation(typeOuterNamespace, typeDecl);
                    }
                }
            }
        }

        void FixNameSpaceUsages(IReadOnlyList<Violation> classDeclarations, CancellationToken cancellationToken)
        {
            foreach (var classDecl in classDeclarations)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(classDecl.ViolatingNode, cancellationToken);
                var symbol = symbolInfo.Symbol;
                if (symbol?.ContainingNamespace != null &&
                    symbol.ContainingNamespace.ToDisplayString() == namespaceName.Value)
                {
                    var fullNameSpace = symbol.ToDisplayString();

                    var possibleMethodCall = classDecl.ViolatingNode.Parent;
                    if (possibleMethodCall is MemberAccessExpressionSyntax)
                    {
                        if (semanticModel.GetSymbolInfo(possibleMethodCall, cancellationToken).Symbol is IMethodSymbol possibleExtensionMethod
                            && possibleExtensionMethod.IsExtensionMethod)
                        {
                            FixForExtensionMethod(symbol);
                        }
                    }
                    else
                    {
                        QualifyUsageOfType(fullNameSpace, classDecl);
                    }
                }
            }
        }

        void QualifyUsageOfType(string fullNameSpace, Violation classDecl)
        {
            var replace = new DottedName(fullNameSpace).SkipCommonPrefix(classDecl.NameSpace);
            if (replace != null)
            {
                var qualifiedName = SyntaxFactory.ParseName(replace.Value)
                    .WithLeadingTrivia(classDecl.ViolatingNode.GetLeadingTrivia())
                    .WithTrailingTrivia(classDecl.ViolatingNode.GetTrailingTrivia());

                // At least some namespace already present - maybe even too much.
                if (classDecl.ViolatingNode.Parent is QualifiedNameSyntax identifierQualifiedNameSyntax)
                {
                    if (identifierQualifiedNameSyntax.ToFullString() != qualifiedName.ToFullString())
                    {
                        editor.ReplaceNode(identifierQualifiedNameSyntax, qualifiedName);
                    }

                    // Else do nothing - already qualified as it should be.
                }
                else
                {
                    editor.ReplaceNode(classDecl.ViolatingNode, qualifiedName);
                }
            }
        }

        void FixForExtensionMethod(ISymbol symbol)
        {
            var staticUsingText = symbol.ContainingType.ToString();

            AddStaticUsingAtMostOnce(staticUsingText);
        }

        void AddStaticUsingAtMostOnce(string staticUsingText)
        {
            var staticUsing = SyntaxFactory.UsingDirective(SyntaxFactory.Token(SyntaxKind.StaticKeyword), null, SyntaxFactory.ParseName(staticUsingText));
            if (staticUsings.Add(staticUsing))
            {
                editor.InsertBefore(usingDirective, staticUsing);
            }
        }

        sealed class Violation
        {
            public Violation(DottedName @namespace, TypeSyntax violatingNode)
            {
                NameSpace = @namespace;
                ViolatingNode = violatingNode;
            }

            public DottedName NameSpace { get; }

            public TypeSyntax ViolatingNode { get; }
        }
    }
}
