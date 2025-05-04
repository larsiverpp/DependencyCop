using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Liversen.DependencyCop.DescendantNamespaceAccess
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class Analyzer : DiagnosticAnalyzer
    {
        public const string RuleId = "DC1002";
        static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            RuleId,
            "Code must not refer code in descendant namespaces",
            "Do not use type '{0}' from descendant namespace '{1}'",
            "DC.Design",
            DiagnosticSeverity.Warning,
            true,
            helpLinkUri: "https://github.com/larsiverpp/DependencyCop/blob/main/Liversen.DependencyCop/Documentation/DC1002.md");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(AnalyseTypeUsage, SyntaxKind.IdentifierName, SyntaxKind.GenericName, SyntaxKind.DefaultLiteralExpression);
        }

        static void AnalyseTypeUsage(SyntaxNodeAnalysisContext context)
        {
            var type = Helpers.DetermineReferredType(context);
            var enclosingType = Helpers.DetermineEnclosingType(context);
            if (type != null && enclosingType != null)
            {
                var typeNamespace = Helpers.ContainingNamespace(type);
                var enclosingTypeNamespace = Helpers.ContainingNamespace(enclosingType);
                if (typeNamespace.IsDescendantOf(enclosingTypeNamespace))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation(), type.Name, typeNamespace.Value));
                }
            }
        }
    }
}
