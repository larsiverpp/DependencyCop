using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Liversen.DependencyCop
{
    public class TypeDeclaration
    {
        public TypeDeclaration(string @namespace, IdentifierNameSyntax node)
        {
            NameSpace = @namespace;
            Node = node;
        }

        public string NameSpace { get; set; }

        public IdentifierNameSyntax Node { get; set; }
    }
}
