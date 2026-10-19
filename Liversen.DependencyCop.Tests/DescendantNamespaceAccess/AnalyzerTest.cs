using System.Threading.Tasks;
using Shouldly;
using Xunit;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Liversen.DependencyCop.DescendantNamespaceAccess.Analyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Liversen.DependencyCop.DescendantNamespaceAccess
{
    public class AnalyzerTest
    {
        [Fact]
        async Task GivenCodeReferringCodeInDescendantNamespace_WhenAnalyzing_ThenDiagnostics()
        {
            var code = EmbeddedResourceHelpers.GetAnalyzerTestData(GetType(), "Default");
            var expected = Verify.Diagnostic()
                .WithLocation(5, 62)
                .WithMessage("Do not use type 'Info' from descendant namespace 'DescendantNamespaceAccessAnalyzer.Bank.Account'");

            await Should.NotThrowAsync(() => TestRunner.Run<Analyzer>(code, expected, TestContext.Current.CancellationToken));
        }
    }
}
