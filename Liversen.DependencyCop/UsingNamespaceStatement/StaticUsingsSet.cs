using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Liversen.DependencyCop.UsingNamespaceStatement
{
    class StaticUsingsSet
    {
        readonly List<SyntaxNode> innerList = new List<SyntaxNode>();

        public static async Task<StaticUsingsSet> GetExisingStaticUsings(Document document, CancellationToken cancellationToken)
        {
            var nodeSet = new StaticUsingsSet();
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            if (root == null)
            {
                return nodeSet;
            }

            var usingDirectives = root.DescendantNodes().OfType<UsingDirectiveSyntax>().Where(x => !string.IsNullOrEmpty(x.StaticKeyword.Text));

            foreach (var directive in usingDirectives)
            {
                nodeSet.Add(directive);
            }

            return nodeSet;
        }

        public bool Add(SyntaxNode usingDirective)
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
