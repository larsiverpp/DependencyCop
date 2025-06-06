using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Liversen.DependencyCop.UsingNamespaceStatement
{
    public class ViolationInformation
    {
        public ViolationInformation(DottedName @namespace, TypeSyntax violatingNode)
        {
            NameSpace = @namespace;
            ViolatingNode = violatingNode;
        }

        public DottedName NameSpace { get; }

        public TypeSyntax ViolatingNode { get; }
    }
}
