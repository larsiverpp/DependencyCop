using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Liversen.DependencyCop;

static class TestRunner
{
    public static async Task Run<TAnalyzer>(
        string code,
        DiagnosticResult expected,
        CancellationToken cancellationToken)
        where TAnalyzer : DiagnosticAnalyzer, new() =>
        await new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
        {
            TestState =
            {
                Sources = { code },
                ExpectedDiagnostics = { expected }
            },
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80
        }.RunAsync(cancellationToken);
}
