using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
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

namespace Liversen.DependencyCop.UsingNamespaceStatement
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FixProvider))]
    [Shared]
    public class FixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(Analyzer.RuleDC1001Id);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            // I do not believe this can ever be false, but the documentation is vague. However, the documentation is clear on the following: If SupportsSyntaxTree is true
            // then GetSyntaxRootAsync will not return null.
            if (!context.Document.SupportsSyntaxTree)
            {
                return;
            }

            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            Debug.Assert(root != null, "Should not be null, since SupportsSyntaxTree is true");

            foreach (var diagnostic in context.Diagnostics)
            {
                var usingDirective = (UsingDirectiveSyntax)root.FindNode(diagnostic.Location.SourceSpan);

                context.RegisterCodeFix(
                CodeAction.Create(
                    title: $"Qualify usages and remove this line ('using {usingDirective.Name};').",
                    createChangedDocument: c => ApplyFixAsync(usingDirective, context.Document, c),
                    equivalenceKey: "QualifyAndRemoveUsing"),
                diagnostic);
            }
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

        // Helper method to find the containing namespace of a given syntax node
        private static string GetContainingNamespace(TypeSyntax node, SemanticModel semanticModel)
        {
            Debug.Assert(semanticModel != null, nameof(semanticModel) + " != null");
            return semanticModel.GetSymbolInfo(node).Symbol?.ContainingNamespace.ToString();
        }

        private async Task<Document> ApplyFixAsync(UsingDirectiveSyntax usingDirective, Document document, CancellationToken cancellationToken)
        {
            try
            {
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
                Debug.Assert(usingDirective.Name != null, "The analyzer will not report any using directives where this is null");
                var namespaceName = usingDirective.Name.ToString();

                var classDeclarations = await FindClassesInNamespaceAsync(namespaceName, document, semanticModel, cancellationToken);
                foreach (var classDecl in classDeclarations)
                {
                    var symbolInfo = semanticModel.GetSymbolInfo(classDecl.Node, cancellationToken);
                    var symbol = symbolInfo.Symbol;
                    if (symbol?.ContainingNamespace != null &&
                        symbol.ContainingNamespace.ToDisplayString() == namespaceName)
                    {
                        var fullNameSpace = symbol.ToDisplayString();

                        // This indicates that it is an extension method.
                        if (classDecl.Node.Parent is MemberAccessExpressionSyntax)
                        {
                            var staticUsing = SyntaxFactory.UsingDirective(SyntaxFactory.Token(SyntaxKind.StaticKeyword), null, SyntaxFactory.ParseName("UsingNamespaceStatementAnalyzer.Account.ItemExtensions"));
                            editor.InsertBefore(usingDirective, staticUsing);
                        }
                        else
                        {
                            var replace = RemoveCommonNameSpace(fullNameSpace, classDecl.NameSpace);
                            NameSyntax qualifiedName = SyntaxFactory.ParseName(replace)
                                .WithLeadingTrivia(classDecl.Node.GetLeadingTrivia())
                                .WithTrailingTrivia(classDecl.Node.GetTrailingTrivia());

                            // At least some namespace already present
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
                }

                // Remove the using directive.
                editor.RemoveNode(usingDirective);

                return editor.GetChangedDocument();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                throw;
            }
        }

        private async Task<IReadOnlyList<TypeDeclaration>> FindClassesInNamespaceAsync(string namespaceName, Document document, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var back = new List<TypeDeclaration>();

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            Debug.Assert(root != null, "Should not be null, since it was checked previously");

            // Find all class declarations in the document
            var nameSpaceDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
            foreach (var namespaceDeclarationSyntax in nameSpaceDeclarations)
            {
                var typeOuterNamespace = semanticModel.GetDeclaredSymbol(namespaceDeclarationSyntax).NamespaceFullName();

                if (typeOuterNamespace == namespaceName)
                {
                    continue;
                }

                var typeDeclarations = namespaceDeclarationSyntax.DescendantNodes().OfType<SimpleNameSyntax>();

                // Filter type declarations that are within the specified namespace
                foreach (var typeDecl in typeDeclarations)
                {
                    var declarationInNamespace = typeOuterNamespace;
                    if (declarationInNamespace != namespaceName)
                    {
                        var containingNamespace = GetContainingNamespace(typeDecl, semanticModel);
                        if (containingNamespace != null && containingNamespace == namespaceName)
                        {
                            back.Add(new TypeDeclaration(declarationInNamespace, typeDecl));
                        }
                    }
                }
            }

            return back;
        }
    }
}
