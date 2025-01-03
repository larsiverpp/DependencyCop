using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Liversen.DependencyCop
{
    public class TypeDeclaration
    {
        public TypeDeclaration(string @namespace, TypeSyntax node)
        {
            NameSpace = @namespace;
            Node = node;
        }

        public string NameSpace { get; set; }

        public TypeSyntax Node { get; set; }
    }
}
