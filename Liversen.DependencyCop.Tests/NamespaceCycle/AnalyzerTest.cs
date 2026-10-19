using System.Threading.Tasks;
using Shouldly;
using Xunit;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Liversen.DependencyCop.NamespaceCycle.Analyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Liversen.DependencyCop.NamespaceCycle
{
    public class AnalyzerTest
    {
        [Fact]
        public async Task GivenCodeWithCycle_WhenAnalyzing_ThenDiagnostics()
        {
            var code = EmbeddedResourceHelpers.GetAnalyzerTestData(GetType(), "Default");
            var expected = Verify.Diagnostic()
                .WithLocation(14, 28)
                .WithLocation(22, 24)
                .WithMessage("Break up namespace cycle 'NamespaceCycleAnalyzer.Account->NamespaceCycleAnalyzer.Transaction->NamespaceCycleAnalyzer.Account'");

            await Should.NotThrowAsync(() => TestRunner.Run<Analyzer>(code, expected, TestContext.Current.CancellationToken));
        }
    }
}
