using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Swap.Htmx.Generators;
using System.Collections.Immutable;
using System.Reflection;

namespace Swap.Htmx.Generators.Tests;

public static class GeneratorTestHelper
{
    public static string? GetGeneratedOutput(string source)
    {
        return GetGeneratedOutput<EventSourceGenerator>(source);
    }

    public static string? GetGeneratedOutput<TGenerator>(string source, IEnumerable<AdditionalText>? additionalTexts = null)
        where TGenerator : IIncrementalGenerator, new()
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

        var generator = new TGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator)
            .AddAdditionalTexts(additionalTexts?.ToImmutableArray() ?? ImmutableArray<AdditionalText>.Empty);

        driver = driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        var generatedResult = runResult.Results.FirstOrDefault();

        if (generatedResult.GeneratedSources.IsEmpty)
            return null;

        return generatedResult.GeneratedSources.First().SourceText.ToString();
    }
}

/// <summary>
/// In-memory additional text for testing generators that use AdditionalTextsProvider.
/// </summary>
public class InMemoryAdditionalText : AdditionalText
{
    private readonly string _path;
    private readonly string _content;

    public InMemoryAdditionalText(string path, string content)
    {
        _path = path;
        _content = content;
    }

    public override string Path => _path;

    public override SourceText? GetText(CancellationToken cancellationToken = default)
    {
        return SourceText.From(_content);
    }
}
