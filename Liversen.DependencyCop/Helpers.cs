using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Liversen.DependencyCop
{
    static class Helpers
    {
        public static ITypeSymbol? DetermineReferredType(SyntaxNodeAnalysisContext context)
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

        public static ITypeSymbol? DetermineEnclosingType(SyntaxNodeAnalysisContext context)
        {
            var node = context.Node;
            var semanticModel = context.SemanticModel;
            var typeDeclarationSyntaxNode = node.AncestorsAndSelf().FirstOrDefault(i => i.IsTypeDeclaration());
            if (typeDeclarationSyntaxNode == null)
            {
                return null;
            }
            return semanticModel.GetDeclaredSymbol(typeDeclarationSyntaxNode) as ITypeSymbol;
        }

        public static (string Left, string Right) RemoveCommonNamespacePrefix(string left, string right)
        {
            var leftParts = left.Split('.');
            var rightParts = right.Split('.');
            for (var i = 0; i < Math.Min(leftParts.Length, rightParts.Length); ++i)
            {
                if (leftParts[i] != rightParts[i])
                {
                    return (string.Join(".", leftParts.Take(i + 1)), string.Join(".", rightParts.Take(i + 1)));
                }
            }
            return (string.Empty, string.Empty);
        }
    }
}
