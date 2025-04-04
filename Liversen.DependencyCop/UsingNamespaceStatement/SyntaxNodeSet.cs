using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Liversen.DependencyCop.UsingNamespaceStatement
{
    /// <summary>
    /// Does not allow duplicate items. Uses <see cref="SyntaxNode.IsEquivalentTo(Microsoft.CodeAnalysis.SyntaxNode)"/> to determine equality.
    /// </summary>
    internal class SyntaxNodeSet
    {
        readonly List<SyntaxNode> innerList = new List<SyntaxNode>();

        public bool TryAdd(SyntaxNode usingDirective)
        {
            if (innerList.Exists(x => x.IsEquivalentTo(usingDirective, true)))
            {
                return false;
            }

            innerList.Add(usingDirective);
            return true;
        }
    }
}
