using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Swap.Htmx.Generators;
using System.Collections.Immutable;
using System.Reflection;

namespace Swap.Htmx.Generators.Tests;

public static class GeneratorTestHelper
{
    public static string? GetGeneratedOutput(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location)
        };

        var compilation = CSharpCompilation.Create(
            "Tests",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new EventSourceGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        var generatedResult = runResult.Results.FirstOrDefault();

        if (generatedResult.GeneratedSources.IsEmpty)
            return null;

        return generatedResult.GeneratedSources.First().SourceText.ToString();
    }
}
