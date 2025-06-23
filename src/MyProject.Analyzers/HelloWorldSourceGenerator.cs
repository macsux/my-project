using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MyProject.Analyzers.Authoring;
using Newtonsoft.Json.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace MyProject.Analyzers;

[Generator]
public sealed class HelloWorldSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    { 
        var partialTypes = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: (node, _) =>
            {
                return node is TypeDeclarationSyntax cds && cds.Modifiers.Any(m => m.IsKind(PartialKeyword));
            },
            transform: (syntaxContext, _) =>
            {
                
                var type =  (TypeDeclarationSyntax)syntaxContext.Node;
                return new TemplateModel(type)
                {
                    PublicMethods = type.Members
                        .OfType<MethodDeclarationSyntax>()
                        .Where(methodDeclaration => methodDeclaration.Modifiers.Any(x => x.IsKind(PublicKeyword)))
                        .Select(x => x.Identifier.ToString())
                        .ToList(),
                };
            });

        context.RegisterSourceOutput(partialTypes, (spc, templateModel) =>
        {
            templateModel.RenderSourceFile(RenderBody, spc);
        });

    }

    private string RenderBody(TemplateModel model)
    {
        var body = $$"""
            {{model.PublicMethods.RenderStatements(publicMethod => $$"""
                //{{publicMethod}}
                """)}}
            """;
        return body;
    }

    private class TemplateModel(TypeDeclarationSyntax partialType) : PartialTypeModel(partialType)
    {
        public required List<string> PublicMethods { get; set; }
    }
    
}