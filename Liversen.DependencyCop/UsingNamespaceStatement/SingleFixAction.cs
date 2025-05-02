using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Liversen.DependencyCop.UsingNamespaceStatement
{
    internal class SingleFixAction
    {
        private readonly SemanticModel semanticModel;
        private readonly DocumentEditor editor;
        private readonly string namespaceName;
        private readonly Document document;
        private readonly UsingDirectiveSyntax usingDirective;
        readonly HashSet<string> staticUsings = new HashSet<string>();

        SingleFixAction(SemanticModel semanticModel, DocumentEditor editor, string namespaceName, Document document, UsingDirectiveSyntax usingDirective)
        {
            this.semanticModel = semanticModel;
            this.editor = editor;
            this.namespaceName = namespaceName;
            this.document = document;
            this.usingDirective = usingDirective;
        }

        public static async Task<Document> ApplyFixAsync(UsingDirectiveSyntax usingDirective, Document document, CancellationToken cancellationToken)
        {
            var semanticModel = await Csharp.GetSemanticModelAsync(document, cancellationToken);
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken);
            Debug.Assert(usingDirective.Name != null, "The analyzer will not report any using directives where this is null");
            var namespaceName = usingDirective.Name.ToString();

            var fixAction = new SingleFixAction(semanticModel, editor, namespaceName, document, usingDirective);

            return await fixAction.ApplyFix(cancellationToken);
        }

        private static string RemoveCommonNameSpace(string originalNameSpace, string compareNameSpace)
        {
            var common = new StringBuilder();

            var nameSpace1Parts = originalNameSpace.Split('.');
            var nameSpace2Parts = compareNameSpace.Split('.');

            for (int i = 0; i < nameSpace1Parts.Length; i++)
            {
                if (nameSpace1Parts[i] == nameSpace2Parts[i])
                {
                    common.Append(nameSpace1Parts[i] + ".");
                }
                else
                {
                    break;
                }
            }

            return originalNameSpace.Substring(common.Length);
        }

        private async Task<Document> ApplyFix(CancellationToken cancellationToken)
        {
            var classDeclarations = await FindNameSpaceUsagesAsync(cancellationToken);
            FixNameSpaceUsages(classDeclarations, cancellationToken);

            editor.RemoveNode(usingDirective);

            return editor.GetChangedDocument();
        }

        private async Task<List<ViolationInformation>> FindNameSpaceUsagesAsync(CancellationToken cancellationToken)
        {
            var back = new List<ViolationInformation>();

            var root = await Csharp.GetSyntaxRootAsync(document, cancellationToken);

            var nameSpaceDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
            foreach (var namespaceDeclarationSyntax in nameSpaceDeclarations)
            {
                var declaredSymbol = Csharp.GetDeclaredSymbol(semanticModel, namespaceDeclarationSyntax);

                var typeOuterNamespace = declaredSymbol.NamespaceFullName() ?? string.Empty;

                if (typeOuterNamespace == namespaceName)
                {
                    continue;
                }

                var typeDeclarations = namespaceDeclarationSyntax.DescendantNodes().OfType<SimpleNameSyntax>();

                back.AddRange(FilterTypeDeclarationWithinSpecifiedNamespace(typeDeclarations, typeOuterNamespace));
            }

            return back;
        }

        IEnumerable<ViolationInformation> FilterTypeDeclarationWithinSpecifiedNamespace(IEnumerable<SimpleNameSyntax> typeDeclarations, string typeOuterNamespace)
        {
            foreach (var typeDecl in typeDeclarations)
            {
                var declarationInNamespace = typeOuterNamespace;
                if (declarationInNamespace != namespaceName)
                {
                    var containingNamespace = typeDecl.GetContainingNamespace(semanticModel);
                    if (containingNamespace != null && containingNamespace == namespaceName)
                    {
                        yield return new ViolationInformation(declarationInNamespace, typeDecl);
                    }
                }
            }
        }

        private void FixNameSpaceUsages(IReadOnlyList<ViolationInformation> classDeclarations, CancellationToken cancellationToken)
        {
            foreach (var classDecl in classDeclarations)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(classDecl.ViolatingNode, cancellationToken);
                var symbol = symbolInfo.Symbol;
                if (symbol?.ContainingNamespace != null &&
                    symbol.ContainingNamespace.ToDisplayString() == namespaceName)
                {
                    var fullNameSpace = symbol.ToDisplayString();

                    // This indicates that it is an extension method.
                    if (classDecl.ViolatingNode.Parent is MemberAccessExpressionSyntax)
                    {
                        FixForExtensionMethod(symbol);
                    }
                    else
                    {
                        QualifyUsageOfType(fullNameSpace, classDecl);
                    }
                }
            }
        }

        private void QualifyUsageOfType(string fullNameSpace, ViolationInformation classDecl)
        {
            var replace = RemoveCommonNameSpace(fullNameSpace, classDecl.NameSpace);
            NameSyntax qualifiedName = SyntaxFactory.ParseName(replace)
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

        private void FixForExtensionMethod(ISymbol symbol)
        {
            var staticUsingText = symbol.ContainingType.ToString();

            AddStaticUsingAtMostOnce(staticUsingText);
        }

        void AddStaticUsingAtMostOnce(string staticUsingText)
        {
            if (staticUsings.Add(staticUsingText))
            {
                var staticUsing = SyntaxFactory.UsingDirective(SyntaxFactory.Token(SyntaxKind.StaticKeyword), null, SyntaxFactory.ParseName(staticUsingText));
                editor.InsertBefore(usingDirective, staticUsing);
            }
        }
    }
}
