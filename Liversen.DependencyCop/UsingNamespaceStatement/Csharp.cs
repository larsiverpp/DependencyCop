using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Liversen.DependencyCop.UsingNamespaceStatement
{
    /// <summary>
    /// Helper class where we ignore nullable in cases where we know the value is not null, given our context.
    /// Our context is that this is regular C# code (hence not vb and not dynamic code generated code).
    /// </summary>
    internal static class Csharp
    {
        public static bool IsNormalCsharpCode(Document document)
        {
            return document.SupportsSyntaxTree && document is { SupportsSemanticModel: true, SourceCodeKind: SourceCodeKind.Regular };
        }

        public static async Task<SemanticModel> GetSemanticModelAsync(Document document, CancellationToken cancellationToken)
        {
            return (await document.GetSemanticModelAsync(cancellationToken))!;
        }

        public static async Task<SyntaxNode> GetSyntaxRootAsync(Document document, CancellationToken cancellationToken)
        {
            return (await document.GetSyntaxRootAsync(cancellationToken))!;
        }

        public static INamedTypeSymbol GetDeclaredSymbol(SemanticModel semanticModel, ClassDeclarationSyntax declarationSyntax)
        {
            return semanticModel.GetDeclaredSymbol(declarationSyntax)!;
        }
    }
}
