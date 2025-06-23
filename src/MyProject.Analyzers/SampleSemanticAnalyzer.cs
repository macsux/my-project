using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MyProject.Analyzers;


[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SampleSemanticAnalyzer : DiagnosticAnalyzer
{

    // Preferred format of DiagnosticId is Your Prefix + Number, e.g. CA1234.
    private const string DiagnosticId = "AB0001";

    private static readonly LocalizableString Title = "MyProject Fancy Analyzer";

    // The message that will be displayed to the user.
    private static readonly LocalizableString MessageFormat = "Something in {0} should not be {1}";

    private static readonly LocalizableString Description = "Does something fancy.";

    // The category of the diagnostic (Design, Naming etc.).
    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor Rule = new(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

    // Keep in mind: you have to list your rules here.
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [Rule];

    public override void Initialize(AnalysisContext context)
    {
        // You must call this method to avoid analyzing generated code.
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        // You must call this method to enable the Concurrent Execution.
        context.EnableConcurrentExecution();

        // best use AI to help implement the logic here
    }
}