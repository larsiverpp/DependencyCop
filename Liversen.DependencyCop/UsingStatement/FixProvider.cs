using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Liversen.DependencyCop.FixProvider
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FixProvider))]
    [Shared]
    public class FixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("DC1001");

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the using statement identified by the diagnostic.
            var usingDirective = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<UsingDirectiveSyntax>().First();

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: $"Qualify usages and remove this line ('using {usingDirective.Name};').",
                    createChangedDocument: c => ApplyFixAsync(usingDirective, context.Document, c),
                    equivalenceKey: "QualifyAndRemoveUsing"),
                diagnostic);
        }

        private async Task<Document> ApplyFixAsync(UsingDirectiveSyntax usingDirective, Document document, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var namespaceName = usingDirective.Name.ToString();

            var classDeclarations = await FindClassesInNamespaceAsync(usingDirective, document, semanticModel, cancellationToken);
            foreach (var classDecl in classDeclarations)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(classDecl.Node, cancellationToken);
                var symbol = symbolInfo.Symbol;

                if (symbol?.ContainingNamespace != null &&
                    symbol.ContainingNamespace.ToDisplayString() == namespaceName)
                {
                    var fullNameSpace = symbol.ToDisplayString();
                    var replace = RemoveCommonNameSpace(fullNameSpace, classDecl.NameSpace);
                    NameSyntax qualifiedName = SyntaxFactory.ParseName(replace)
                        .WithLeadingTrivia(classDecl.Node.GetLeadingTrivia())
                        .WithTrailingTrivia(classDecl.Node.GetTrailingTrivia());

                    if (classDecl.Node.Parent is QualifiedNameSyntax identifierQualifiedNameSyntax)
                    {
                        if (identifierQualifiedNameSyntax.ToFullString() != qualifiedName.ToFullString())
                        {
                            editor.ReplaceNode(identifierQualifiedNameSyntax, qualifiedName);
                        }

                        // Else do nothing - already qualified enough.
                    }
                    else
                    {
                        editor.ReplaceNode(classDecl.Node, qualifiedName);
                    }
                }
            }

            // Remove the using directive.
            editor.RemoveNode(usingDirective);

            return editor.GetChangedDocument();
        }

        private async Task<IReadOnlyList<TypeDeclaration>> FindClassesInNamespaceAsync(UsingDirectiveSyntax usingDirective, Document document, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var back = new List<TypeDeclaration>();

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var namespaceName = usingDirective.Name.ToString();

            // Find all class declarations in the document
            var nameSpaceDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
            foreach (var namespaceDeclarationSyntax in nameSpaceDeclarations)
            {
#pragma warning disable RS1035
                var typeOuterNamespace = semanticModel.GetDeclaredSymbol(namespaceDeclarationSyntax).NamespaceFullName().Replace(Environment.NewLine, string.Empty);
#pragma warning restore RS1035
                if (typeOuterNamespace == namespaceName)
                {
                    continue;
                }

                var typeDeclarations = namespaceDeclarationSyntax.DescendantNodes().OfType<IdentifierNameSyntax>();

                // Filter type declarations that are within the specified namespace
                foreach (var typeDecl in typeDeclarations)
                {
                    var declarationInNamespace = typeOuterNamespace;
                    if (declarationInNamespace != namespaceName)
                    {
                        var containingNamespace = GetContainingNamespace(typeDecl, semanticModel);
                        if (containingNamespace != null && containingNamespace.ToString() == namespaceName)
                        {
                            back.Add(new TypeDeclaration(declarationInNamespace, typeDecl));
                        }
                    }
                }
            }

            return back;
        }

        // Helper method to find the containing namespace of a given syntax node
        private string GetContainingNamespace(IdentifierNameSyntax node, SemanticModel semanticModel)
        {
            return semanticModel.GetTypeInfo(node).Type?.NamespaceFullName();
        }

#pragma warning disable SA1204
        private static string RemoveCommonNameSpace(string originalNameSpace, string compareNameSpace)
#pragma warning restore SA1204
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
    }
}
