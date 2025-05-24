using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Liversen.DependencyCop
{
    static class Helpers
    {
        public static DottedName ContainingNamespace(ISymbol typeSymbol) =>
            typeSymbol.ContainingNamespace != null
                ? new DottedName(typeSymbol.ContainingNamespace.ToString())
                : null;

        public static ITypeSymbol DetermineReferredType(SyntaxNodeAnalysisContext context)
        {
            var node = context.Node;
            var semanticModel = context.SemanticModel;
            var typeSymbol = semanticModel.GetTypeInfo(node).Type;
            if (typeSymbol != null)
            {
                return typeSymbol;
            }

            var symbolInfo = semanticModel.GetSymbolInfo(node);
            switch (symbolInfo.Symbol)
            {
                case ITypeSymbol symbolInfoTypeSymbol:
                    return symbolInfoTypeSymbol;
                case IMethodSymbol methodSymbol:
                    return methodSymbol.ReturnType;
                default:
                    return null;
            }
        }

        public static ITypeSymbol DetermineEnclosingType(SyntaxNodeAnalysisContext context)
        {
            var node = context.Node;
            var semanticModel = context.SemanticModel;
            var typeDeclarationSyntaxNode = node.AncestorsAndSelf()
                .FirstOrDefault(syntaxNode => syntaxNode is BaseTypeDeclarationSyntax || syntaxNode is DelegateDeclarationSyntax);
            if (typeDeclarationSyntaxNode == null)
            {
                return null;
            }

            return semanticModel.GetDeclaredSymbol(typeDeclarationSyntaxNode) as ITypeSymbol;
        }

        public static bool TypesInSameAssembly(ITypeSymbol type1, ITypeSymbol type2) =>
            type1.ContainingAssembly != null
            && type2.ContainingAssembly != null
            && AssemblyIdentityComparer.Default.Compare(
                type1.ContainingAssembly.Identity,
                type2.ContainingAssembly.Identity)
            == AssemblyIdentityComparer.ComparisonResult.Equivalent;
    }
}
