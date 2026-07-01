using System.IO;
using Swap.Htmx.Models;
using Xunit;

namespace Swap.Htmx.Tests;

/// <summary>
/// Guards that the packable assemblies ship their XML documentation, so consumer IntelliSense and
/// NuGet metadata actually receive the authored doc comments.
/// </summary>
public class DocumentationOutputTests
{
    [Fact]
    public void SwapHtmx_ShipsXmlDocumentationFile()
    {
        var assembly = typeof(SwapResponseBuilder).Assembly;
        var xmlPath = System.IO.Path.ChangeExtension(assembly.Location, ".xml");

        Assert.True(File.Exists(xmlPath),
            $"Expected XML documentation next to the assembly at '{xmlPath}'. " +
            "GenerateDocumentationFile must be enabled so IntelliSense/NuGet receive the doc comments.");
    }
}
