using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Liversen.DependencyCop.UsingNamespaceStatement
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FixProvider))]
    [Shared]
    public class FixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(Analyzer.RuleId);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;

            if (!SupportedDocument(document))
            {
                return;
            }

            var rootNode = await document.GetSyntaxRootAsync(CancellationToken.None);
            if (rootNode == null)
            {
                return;
            }

            foreach (var diagnostic in context.Diagnostics)
            {
                var syntaxNode = rootNode.FindNode(diagnostic.Location.SourceSpan);
                if (syntaxNode is UsingDirectiveSyntax usingDirective)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: $"Qualify usages and remove this line ('using {usingDirective.Name};').",
                            createChangedDocument: c => Fixer.Fix(document, usingDirective, c),
                            equivalenceKey: "QualifyAndRemoveUsing"),
                        diagnostic);
                }
            }
        }

        static bool SupportedDocument(Document document) =>
            document.SupportsSyntaxTree && document.SupportsSemanticModel && document.SourceCodeKind == SourceCodeKind.Regular;
    }
}
