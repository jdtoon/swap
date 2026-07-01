using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Swap.Htmx.Generators;
using System.Collections.Immutable;
using Xunit;

namespace Swap.Htmx.Generators.Tests;

public class AutoScanGeneratorTests
{
    private static readonly string FixedSource = @"
namespace TestNamespace;

public class Marker { }
";

    private static AdditionalText[] FixedAdditionalTexts() => new AdditionalText[]
    {
        new InMemoryAdditionalText("C:/Project/Views/Products/Index.cshtml", "<div id=\"product-grid\"></div>"),
        new InMemoryAdditionalText("C:/Project/Views/Products/_ProductRow.cshtml", "<div id=\"product-row\"></div>"),
        new InMemoryAdditionalText("C:/Project/Views/Shared/_Layout.cshtml", "<main id=\"main-content\"></main>"),
    };

    private static Compilation CreateCompilation(string source, string assemblyName = "TestAssembly")
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        };

        return CSharpCompilation.Create(
            assemblyName,
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static GeneratorDriver CreateDriver(IEnumerable<AdditionalText> additionalTexts)
    {
        var generator = new AutoScanGenerator();
        return CSharpGeneratorDriver.Create(
            generators: ImmutableArray.Create(generator.AsSourceGenerator()),
            additionalTexts: ImmutableArray.CreateRange(additionalTexts),
            driverOptions: new GeneratorDriverOptions(default, trackIncrementalGeneratorSteps: true));
    }

    [Fact]
    public void Generates_SwapViews_And_SwapElements_For_Fixed_Set()
    {
        var compilation = CreateCompilation(FixedSource);
        var driver = CreateDriver(FixedAdditionalTexts());

        driver = driver.RunGenerators(compilation);
        var result = driver.GetRunResult().Results.Single();

        Assert.Equal(2, result.GeneratedSources.Length);

        var swapViews = result.GeneratedSources.Single(s => s.HintName == "SwapViews.g.cs").SourceText.ToString();
        var swapElements = result.GeneratedSources.Single(s => s.HintName == "SwapElements.g.cs").SourceText.ToString();

        Assert.Contains("namespace TestAssembly", swapViews);
        Assert.Contains("public static class Products", swapViews);
        Assert.Contains("public const string Index = \"Index\";", swapViews);
        Assert.Contains("public const string _ProductRow = \"_ProductRow\";", swapViews);
        Assert.Contains("public static class Shared", swapViews);
        Assert.Contains("public const string _Layout = \"_Layout\";", swapViews);

        Assert.Contains("namespace TestAssembly", swapElements);
        Assert.Contains("public const string ProductGrid = \"product-grid\";", swapElements);
        Assert.Contains("public const string ProductRow = \"product-row\";", swapElements);
        Assert.Contains("public const string MainContent = \"main-content\";", swapElements);
    }

    [Fact]
    public void Editing_Unrelated_Compilation_Does_Not_Rerun_File_Scan_Pipeline()
    {
        var additionalTexts = FixedAdditionalTexts();
        var driver = CreateDriver(additionalTexts);

        var compilation1 = CreateCompilation(FixedSource);
        driver = driver.RunGenerators(compilation1);
        var firstRunOutput = driver.GetRunResult().Results.Single()
            .GeneratedSources.Single(s => s.HintName == "SwapViews.g.cs").SourceText.ToString();

        // Simulate an edit to an unrelated .cs file: same assembly name, different syntax tree.
        var compilation2 = CreateCompilation(FixedSource + "\npublic class AnotherUnrelatedClass { }");
        driver = driver.RunGenerators(compilation2);

        var runResult = driver.GetRunResult().Results.Single();
        var secondRunOutput = runResult
            .GeneratedSources.Single(s => s.HintName == "SwapViews.g.cs").SourceText.ToString();

        // Generated output must be unchanged (behavior-preserving).
        Assert.Equal(firstRunOutput, secondRunOutput);

        // The per-file scan step and its downstream collection must be fully cached -
        // the unrelated compilation edit should not have re-run the .cshtml scan.
        var scannedFileSteps = runResult.TrackedSteps[AutoScanGenerator.TrackingNames.ScannedFiles];
        Assert.All(scannedFileSteps, step => Assert.All(step.Outputs, o => Assert.Equal(IncrementalStepRunReason.Cached, o.Reason)));

        var collectedFilesSteps = runResult.TrackedSteps[AutoScanGenerator.TrackingNames.CollectedFiles];
        Assert.All(collectedFilesSteps, step => Assert.All(step.Outputs, o => Assert.Equal(IncrementalStepRunReason.Cached, o.Reason)));

        // The assembly name projection is unmodified, so the combined step and final
        // source output should also be cached (i.e. skipped entirely).
        var assemblyNameSteps = runResult.TrackedSteps[AutoScanGenerator.TrackingNames.AssemblyName];
        Assert.All(assemblyNameSteps, step => Assert.All(step.Outputs, o => Assert.Equal(IncrementalStepRunReason.Unchanged, o.Reason)));

        var combinedSteps = runResult.TrackedSteps[AutoScanGenerator.TrackingNames.Combined];
        Assert.All(combinedSteps, step => Assert.All(step.Outputs, o => Assert.Equal(IncrementalStepRunReason.Cached, o.Reason)));
    }

    [Fact]
    public void Editing_Assembly_Name_Regenerates_With_New_Namespace()
    {
        var additionalTexts = FixedAdditionalTexts();
        var driver = CreateDriver(additionalTexts);

        var compilation1 = CreateCompilation(FixedSource, assemblyName: "FirstAssembly");
        driver = driver.RunGenerators(compilation1);
        var firstRunOutput = driver.GetRunResult().Results.Single()
            .GeneratedSources.Single(s => s.HintName == "SwapViews.g.cs").SourceText.ToString();

        var compilation2 = CreateCompilation(FixedSource, assemblyName: "SecondAssembly");
        driver = driver.RunGenerators(compilation2);
        var secondRunOutput = driver.GetRunResult().Results.Single()
            .GeneratedSources.Single(s => s.HintName == "SwapViews.g.cs").SourceText.ToString();

        Assert.Contains("namespace FirstAssembly", firstRunOutput);
        Assert.Contains("namespace SecondAssembly", secondRunOutput);
        Assert.NotEqual(firstRunOutput, secondRunOutput);
    }
}
