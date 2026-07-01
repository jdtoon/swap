using System.IO;
using Xunit;

namespace Swap.Htmx.Tests;

/// <summary>
/// Guards that the project templates register the Swap.Htmx tag helpers in their _ViewImports, so a
/// scaffolded app renders &lt;swap-*&gt; helpers instead of emitting them as literal text.
/// </summary>
public class TemplateViewImportsTests
{
    [Theory]
    [InlineData("templates/content/Swap.Mvc/Views/_ViewImports.cshtml")]
    [InlineData("templates/content/Swap.ModularMonolith/src/Views/_ViewImports.cshtml")]
    public void Template_ViewImports_RegistersSwapTagHelpers(string relativePath)
    {
        var repoRoot = FindRepoRoot();
        var path = Path.Combine(repoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));

        Assert.True(File.Exists(path), $"Expected template _ViewImports at '{path}'.");
        var content = File.ReadAllText(path);

        Assert.Contains("@addTagHelper *, Swap.Htmx", content);
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "swap.sln")))
                return current.FullName;

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repo root (swap.sln not found).");
    }
}
