using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Liversen.DependencyCop.UsingNamespaceStatement
{
    public class ViolationInformation
    {
        public ViolationInformation(string @namespace, TypeSyntax violatingNode)
        {
            NameSpace = @namespace;
            ViolatingNode = violatingNode;
        }

        public string NameSpace { get; }

        public TypeSyntax ViolatingNode { get; }
    }
}
