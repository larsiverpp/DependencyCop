using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Liversen.DependencyCop.NamespaceCycle
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class Analyzer : DiagnosticAnalyzer
    {
        public const string RuleId = "DC1003";
        static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            RuleId,
            "Code must not contain namespace cycles",
            "Break up namespace cycle '{0}'",
            "DC.Design",
            DiagnosticSeverity.Warning,
            true,
            helpLinkUri: "https://github.com/larsiverpp/DependencyCop/blob/main/Liversen.DependencyCop/Documentation/DC1003.md");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1026:Enable concurrent execution", Justification = "Cannot have two simultaneous threads change state at the same time.")]
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterCompilationStartAction(compilationContext =>
            {
                var inner = new Inner();
                compilationContext.RegisterSyntaxNodeAction(inner.Analyse, SyntaxKind.IdentifierName, SyntaxKind.GenericName, SyntaxKind.DefaultLiteralExpression);
            });
        }

        class Inner
        {
            readonly Dag dag = new Dag();

            public void Analyse(SyntaxNodeAnalysisContext context)
            {
                var sourceType = Helpers.DetermineEnclosingType(context);
                var targetType = Helpers.DetermineReferredType(context);
                if (sourceType != null && targetType != null && SameAssembly(sourceType, targetType))
                {
                    var sourceNamespace = Helpers.NamespaceFullName(sourceType);
                    var targetNamespace = Helpers.NamespaceFullName(targetType);
                    var dependency = DottedName.TakeIncludingFirstDifferingPart(sourceNamespace, targetNamespace);
                    if (dependency != null)
                    {
                        var (source, target) = dependency.Value;
                        var cycle = dag.TryAddVertex(source.Value, target.Value);
                        if (cycle != null)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Node.GetLocation(), string.Join("->", cycle)));
                        }
                    }
                }
            }

            static bool SameAssembly(ITypeSymbol left, ITypeSymbol right) =>
                left.ContainingAssembly != null
                && right.ContainingAssembly != null
                && AssemblyIdentityComparer.Default.Compare(
                    left.ContainingAssembly.Identity,
                    right.ContainingAssembly.Identity)
                == AssemblyIdentityComparer.ComparisonResult.Equivalent;
        }
    }
}
