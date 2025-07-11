using Microsoft.CodeAnalysis.CSharp.Syntax;
using MyProject.Analyzers.Authoring;

namespace MyProject.Analyzers.CovariantReturns;

public sealed record CovariantInterfaceModel : PartialTypeModel
{
    public CovariantInterfaceModel(TypeDeclarationSyntax partialType, string fileName) : base(partialType, fileName)
    {

    }
    // public List<MethodToGenerate>();
    
    public required string Name { get; init; }
    public required ImmutableEquatableArray<MethodToGenerate> MethodsToGenerate { get; init; }
    // public required ImmutableEquatableArray<TypeParameterModel> TypeParameters { get; init; }
    // public required ImmutableEquatableArray<CovariantMethodModel> Methods { get; init; }
    // public required ImmutableEquatableArray<CovariantInterfaceModel> BaseInterfaces { get; init; }


    public bool Equals(CovariantInterfaceModel? other) =>
        other is not null &&
        Name == other.Name &&
        FileName == other.FileName &&
        PartialDeclarationTemplate == other.PartialDeclarationTemplate &&
        IdentLevel == other.IdentLevel &&
        MethodsToGenerate.Equals(other.MethodsToGenerate) 
        // TypeParameters.Equals(other.TypeParameters) &&
        // Methods.Equals(other.Methods) &&
        // BaseInterfaces.Equals(other.BaseInterfaces)
        ;
    
    public override int GetHashCode() => Hash
        .Builder(Name)
        .Combine(FileName)
        .Combine(PartialDeclarationTemplate)
        .CombineValue(IdentLevel)
        .Combine(MethodsToGenerate)
        // .Combine(TypeParameters)
        // .Combine(Methods)
        // .Combine(BaseInterfaces)
        .HashCode;


}

public record MethodToGenerate
{
    public required GenerationType TemplateType { get; init; }
    public required string Name { get; init; }
    public required string ReturnType { get; init; }
    public required ImmutableEquatableArray<ParameterModel> Parameters { get; init; }
    public Redirect? RedirectToType { get; init; }

    public enum GenerationType
    {
        /// <summary>
        /// Use on child interfaces redeclaring parent methods with more specific return types.
        /// Should have a matching <see cref="ExplicitInterfaceRedirectToSelf"/> 
        /// </summary>
        /// <example>
        ///   public new IChild Self();
        /// </example>
        NewCovariant,
        /// <summary>
        /// Used when parent interface provides default implementation, and child interface redeclares a more specific method that redirects
        /// to parent interface default implementation
        /// </summary>
        /// <example>
        ///   public new IChildWithParentDefault Self() => (IChildWithParentDefault)((IParentWithDefault)this).Self();
        /// </example>
        NewCovariantWithParentRedirect,
        
        // on class
        /// <summary>
        /// Used on classes that override a virtual parent method and redirect to parent implementation
        /// </summary>
        /// <example>
        /// 	public override Child Self() => (Child)base.Self();
        /// </example>
        OverrideBaseMethodRedirectToBase,
        
        // on both
        /// <summary>
        /// Provides explicit parent implementation with redirect to implementation on current type
        /// </summary>
        /// <example>
        /// IParent IParent.Self() => Self();
        /// </example>
        ExplicitInterfaceRedirectToSelf,
        
    }
}

public record Redirect
{
    public required string TargetType { get; init; }
    public required string TargetReturn { get; init; }
    
}

public sealed record TypeParameterModel
{
    public required string Name { get; init; }
    public required ImmutableEquatableArray<string> Constraints { get; init; }

    public bool Equals(TypeParameterModel? other) =>
        other is not null &&
        Name == other.Name &&
        Constraints.Equals(other.Constraints);


    public override int GetHashCode() => Hash.Builder(Name).Combine(Constraints).HashCode;
}

public sealed record CovariantMethodModel
{
    public required string Name { get; init; }
    public required ImmutableEquatableArray<ParameterModel> Parameters { get; init; }
    
    public required bool IsAbstract { get; init; }

    public bool Equals(CovariantMethodModel? other) =>
        other is not null &&
        Name == other.Name &&
        IsAbstract == other.IsAbstract &&
        Parameters.Equals(other.Parameters);


    public override int GetHashCode() => Hash.Builder(Name).CombineValue(IsAbstract).Combine(Parameters).HashCode;
}

public sealed record ParameterModel 
{
    public required string Name { get; init; }
    public required string TypeName { get; init; }

    public bool Equals(ParameterModel? other) =>
        other is not null &&
        Name == other.Name &&
        TypeName == other.TypeName;


    public override int GetHashCode() => Hash.Builder(Name).Combine(TypeName).HashCode;
}