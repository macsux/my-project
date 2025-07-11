using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Spectre.Console;

namespace MyProject.Analyzers.Tests;

public class Tests
{
    [Test]
    public void CodeGenerator()
    {

        var src = """
            public partial interface ITree 
            {
                ITree Self();
            }

            public partial interface ICs : ITree 
            {
                
            }

            public partial interface IChild : ICs 
            {
                 
            }

            public partial class Test : ICs
            {
                public Test Self()
                {
                    throw new NotImplementedException();
                }
            }
            """;
        var srcSyntax = CSharpSyntaxTree.ParseText(src, path: "Test.cs");
        var compilation = CSharpCompilation.Create("TestProject",
            [srcSyntax],
            Basic.Reference.Assemblies.Net90.References.All,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new HelloWorldSourceGenerator();
        var sourceGenerator = generator.AsSourceGenerator();

        // trackIncrementalGeneratorSteps allows to report info about each step of the generator
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: new ISourceGenerator[] { sourceGenerator },
            driverOptions: new GeneratorDriverOptions(default, trackIncrementalGeneratorSteps: true));

        // Run the generator
        driver = driver.RunGenerators(compilation);

        // Update the compilation and rerun the generator
        // compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("// dummy"));
        // driver = driver.RunGenerators(compilation);

        // Assert the driver doesn't recompute the output
        var result = driver.GetRunResult().Results.Single();
        
        AnsiConsole.Write(new Panel(src) { Header = new PanelHeader("User")});
        foreach (var source in result.GeneratedSources)
        {
            AnsiConsole.Write(new Panel(source.SourceText.ToString()) { Header = new PanelHeader(source.HintName)});
        }
        
        compilation = CSharpCompilation.Create("TestProject",
            result.GeneratedSources.Select(x => x.SyntaxTree).Append(srcSyntax),
            Basic.Reference.Assemblies.Net90.References.All,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var diagnostics = compilation.GetDiagnostics();
        foreach (var diagnostic in diagnostics)
        {
            Console.WriteLine(diagnostic.ToString());
        }
        
        // var allOutputs = result.TrackedOutputSteps.SelectMany(outputStep => outputStep.Value).SelectMany(output => output.Outputs);
        //
        // // Assert.Collection(allOutputs, output => Assert.Equal(IncrementalStepRunReason.Cached, output.Reason));
        // allOutputs.Should().OnlyContain(output => output.Reason == IncrementalStepRunReason.Cached);
        //
        // // Assert the driver use the cached result from AssemblyName and Syntax
        // var assemblyNameOutputs = result.TrackedSteps["AssemblyName"].Single().Outputs;
        // // Assert.Collection(assemblyNameOutputs, output => Assert.Equal(IncrementalStepRunReason.Unchanged, output.Reason));
        // assemblyNameOutputs.Should().OnlyContain(output => output.Reason == IncrementalStepRunReason.Unchanged);
        //
        // var syntaxOutputs = result.TrackedSteps["Syntax"].Single().Outputs;
        // // Assert.Collection(syntaxOutputs, output => Assert.Equal(IncrementalStepRunReason.Unchanged, output.Reason));
        // syntaxOutputs.Should().OnlyContain(output => output.Reason == IncrementalStepRunReason.Unchanged);

    }

   
}