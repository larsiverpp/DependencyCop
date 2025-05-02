using System.Collections.Immutable;
using System.ComponentModel.Design.Serialization;
using System.Composition;
using System.Diagnostics;
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
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(Analyzer.RuleDC1001Id);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            if (!Csharp.IsNormalCsharpCode(context.Document))
            {
                return;
            }

            var root = await Csharp.GetSyntaxRootAsync(context.Document, CancellationToken.None);

            foreach (var diagnostic in context.Diagnostics)
            {
                // We know the error is found in a using directive.
                var usingDirective = (UsingDirectiveSyntax)root.FindNode(diagnostic.Location.SourceSpan);

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: $"Qualify usages and remove this line ('using {usingDirective.Name};').",
                        createChangedDocument: c => SingleFixAction.ApplyFixAsync(usingDirective, context.Document, c),
                        equivalenceKey: "QualifyAndRemoveUsing"),
                    diagnostic);
            }
        }
    }
}
