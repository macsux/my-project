using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MyProject.Analyzers.Authoring;

public static partial class Extensions
{
    public static bool InheritsFrom(this INamedTypeSymbol symbol, string type)
    {
        var current = symbol.BaseType;
        while (current != null)
        {
            if (current.Name == type)
                return true;
            current = current.BaseType;
        }
        return false;
    }
    
    public static T? FindParent<T>(this SyntaxNode node) where T : class
    {
        var current = node;
        while(true)
        {
            current = current.Parent;
            if (current == null || current is T)
                return current as T;
        }
    }
    
    
    public static bool HasAttribute(this SyntaxList<AttributeListSyntax> attributes, string name)
    {
        string fullname, shortname;
        var attrLen = "Attribute".Length;
        if (name.EndsWith("Attribute"))
        {
            fullname = name;
            shortname = name.Remove(name.Length - attrLen, attrLen);
        }
        else
        {
            fullname = name + "Attribute";
            shortname = name;
        }

        return attributes.Any(al => al.Attributes.Any(a => a.Name.ToString() == shortname || a.Name.ToString() == fullname));
    }
    /// <summary>
    /// Generates a filename-safe string for a TypeDeclarationSyntax, including namespace, nesting, and generic arity.
    /// Example: Namespace.Outer`1.Inner`2.g.cs
    /// </summary>
    public static string GetInferredFilename(this TypeDeclarationSyntax typeDecl)
    {
        if (typeDecl == null)
            throw new ArgumentNullException(nameof(typeDecl));

        var segments = new List<string>();

        // Gather nested type chain
        var currentType = typeDecl;
        while (currentType != null)
        {
            var name = currentType.Identifier.Text;
            var arity = currentType.TypeParameterList?.Parameters.Count ?? 0;
            if (arity > 0)
                name += "`" + arity;

            segments.Insert(0, Sanitize(name));
            currentType = currentType.Parent as TypeDeclarationSyntax;
        }

        // Prepend namespace if available
        var ns = GetContainingNamespace(typeDecl);
        if (!string.IsNullOrEmpty(ns))
            segments.Insert(0, ns!); // make dot filename-safe if needed

        return string.Join(".", segments);
    }

    private static string? GetContainingNamespace(SyntaxNode? node)
    {
        while (node != null)
        {
            if (node is NamespaceDeclarationSyntax nds)
                return nds.Name.ToString();
            if (node is FileScopedNamespaceDeclarationSyntax fns)
                return fns.Name.ToString();
            node = node.Parent;
        }
        return null;
    }

    private static string Sanitize(string input)
    {
        var sb = new StringBuilder(input.Length);
        foreach (var c in input)
        {
            switch (c)
            {
                case '<':
                case '>':
                case ':':
                case '"':
                case '/':
                case '\\':
                case '|':
                case '?':
                case '*':
                    sb.Append('_');
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }

}