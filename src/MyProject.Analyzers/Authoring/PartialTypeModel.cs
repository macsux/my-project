using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MyProject.Analyzers.Authoring;

[PublicAPI]
public abstract class PartialTypeModel(TypeDeclarationSyntax partialType)
{
    public TypeDeclarationSyntax PartialType { get; } = partialType;
    private string? _fileName;


    public virtual string FileName
    {
        get => _fileName ?? $"{PartialType.GetInferredFilename()}.g.cs";
        set => _fileName = value;
    }
    
}