using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Liversen.DependencyCop.UsingNamespaceStatement
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class Analyzer : DiagnosticAnalyzer
    {
        public const string RuleId = "DC1001";
        const string DotnetDiagnosticOptionName = "dotnet_diagnostic.DC1001_NamespacePrefixes";
        const string BuildPropertyOptionName = "build_property.DC1001_NamespacePrefixes";

        static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            RuleId,
            "Using namespace statements must not reference disallowed namespaces",
            "Do not use '{0}' in a using statement, use fully-qualified names",
            "DC.Readability",
            DiagnosticSeverity.Warning,
            true,
            helpLinkUri: "https://github.com/larsiverpp/DependencyCop/blob/main/Liversen.DependencyCop/Documentation/DC1001.md");

        static readonly DiagnosticDescriptor Descriptor2 = new DiagnosticDescriptor(
            "DC1004",
            "Rule DC1001 is not configured",
            "A list of disallowed namespaces must be configured for rule DC1001",
            "DC.Readability",
            DiagnosticSeverity.Warning,
            true,
            helpLinkUri: "https://github.com/larsiverpp/DependencyCop/blob/main/Liversen.DependencyCop/Documentation/DC1004.md",
            customTags: WellKnownDiagnosticTags.CompilationEnd);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor, Descriptor2);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterCompilationStartAction(CompilationStart);
        }

        static string GetDisallowedNamespacePrefixesValue(AnalyzerOptions options)
        {
            var optionsProvider = options.AnalyzerConfigOptionsProvider;
            if (optionsProvider.GlobalOptions.TryGetValue(DotnetDiagnosticOptionName, out var value1))
            {
                return value1;
            }

            if (optionsProvider.GlobalOptions.TryGetValue(BuildPropertyOptionName, out var value2))
            {
                return value2;
            }

            return null;
        }

        static void CompilationStart(CompilationStartAnalysisContext startContext)
        {
            var disallowedNamespacePrefixesValue = GetDisallowedNamespacePrefixesValue(startContext.Options);
            var disallowedNamespacePrefixes = (disallowedNamespacePrefixesValue?.Trim() ?? string.Empty)
                .Split(',')
                .Select(s => s.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => new DottedName(x))
                .ToImmutableArray();
            startContext.RegisterSyntaxNodeAction(c => AnalyseUsingStatement(c, disallowedNamespacePrefixes), SyntaxKind.UsingDirective);
            if (disallowedNamespacePrefixesValue == null)
            {
                startContext.RegisterCompilationEndAction(c => c.ReportDiagnostic(Diagnostic.Create(Descriptor2, null)));
            }
        }

        static void AnalyseUsingStatement(SyntaxNodeAnalysisContext context, ImmutableArray<DottedName> disallowedNamespacePrefixes)
        {
            if (context.Node is UsingDirectiveSyntax node && string.IsNullOrEmpty(node.StaticKeyword.Text) && node.Name != null)
            {
                var name = new DottedName(node.Name.ToFullString());
                if (disallowedNamespacePrefixes.Any(x => name.IsEqualToOrDescendantOf(x)))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, node.GetLocation(), name));
                }
            }
        }
    }
}
