using System;
using System.Threading.Tasks;
using Xunit;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Liversen.DependencyCop.NamespaceCycle.Analyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Liversen.DependencyCop.NamespaceCycle
{
    public class AnalyzerTest
    {
        // It is non-deterministic which of two different diagnostics is returned.
        [Fact]
        public async Task GivenCodeWithCycle_WhenAnalyzing_ThenDiagnostics()
        {
            var code = EmbeddedResourceHelpers.GetAnalyzerTestData(GetType(), "Default");
            var expected1 = Verify.Diagnostic()
                .WithLocation(14, 28)
                .WithMessage("Break up namespace cycle 'NamespaceCycleAnalyzer.Transaction->NamespaceCycleAnalyzer.Account->NamespaceCycleAnalyzer.Transaction'");
            var expected2 = Verify.Diagnostic()
                .WithLocation(22, 24)
                .WithMessage("Break up namespace cycle 'NamespaceCycleAnalyzer.Account->NamespaceCycleAnalyzer.Transaction->NamespaceCycleAnalyzer.Account'");

            // This is stupid, but it seems the VerifyAnalyzerAsync might or might not give different results when run twice in a row.
            try
            {
                try
                {
                    await Verify.VerifyAnalyzerAsync(code, expected1);
                }
                catch (Exception)
                {
                    await Verify.VerifyAnalyzerAsync(code, expected1);
                }
            }
            catch
            {
                try
                {
                    await Verify.VerifyAnalyzerAsync(code, expected1);
                }
                catch (Exception)
                {
                    await Verify.VerifyAnalyzerAsync(code, expected2);
                }
            }
        }
    }
}
