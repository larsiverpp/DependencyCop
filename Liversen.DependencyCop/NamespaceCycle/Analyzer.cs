using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
            readonly Dictionary<string, Location> dependencyIdToLocation = new Dictionary<string, Location>();

            public void Analyse(SyntaxNodeAnalysisContext context)
            {
                var sourceType = Helpers.DetermineEnclosingType(context);
                var targetType = Helpers.DetermineReferredType(context);
                if (sourceType != null && targetType != null && Helpers.TypesInSameAssembly(sourceType, targetType))
                {
                    var sourceNamespace = Helpers.ContainingNamespace(sourceType);
                    var targetNamespace = Helpers.ContainingNamespace(targetType);
                    if (sourceNamespace != null && targetNamespace != null)
                    {
                        var dependency = DottedName.TakeIncludingFirstDifferingPart(sourceNamespace, targetNamespace);
                        if (dependency != null)
                        {
                            var (source, target) = dependency.Value;
                            var dependencyId = DependencyId(source, target);
                            if (!dependencyIdToLocation.ContainsKey(dependencyId))
                            {
                                dependencyIdToLocation.Add(dependencyId, context.Node.GetLocation());
                            }

                            var cycle = dag.TryAddVertex(source.Value, target.Value);
                            if (cycle != null && cycle.HasValue)
                            {
                                var normalizedCycle = NormalizeCycle(cycle.Value);
                                var locations = Locations(normalizedCycle).ToImmutableArray();
                                context.ReportDiagnostic(Diagnostic.Create(
                                    Descriptor,
                                    locations[0],
                                    locations.Skip(1),
                                    string.Join("->", normalizedCycle)));
                            }
                        }
                    }
                }
            }

            static string DependencyId(DottedName source, DottedName target) =>
                $"{source.Value}->{target.Value}";

            static ImmutableArray<string> NormalizeCycle(ImmutableArray<string> cycle)
            {
                var cycleStart = cycle.OrderBy(x => x).First();
                while (cycle[0] != cycleStart)
                {
                    cycle = cycle.Skip(1).Append(cycle[1]).ToImmutableArray();
                }

                return cycle;
            }

            IEnumerable<Location> Locations(ImmutableArray<string> cycle)
            {
                for (var i = 0; i < cycle.Length - 1; ++i)
                {
                    var source = new DottedName(cycle[i]);
                    var target = new DottedName(cycle[i + 1]);
                    var dependencyId = DependencyId(source, target);
                    yield return dependencyIdToLocation[dependencyId];
                }
            }
        }
    }
}
