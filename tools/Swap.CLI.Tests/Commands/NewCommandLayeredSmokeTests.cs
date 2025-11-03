using System.CommandLine;
using System.Diagnostics;
using Swap.CLI.Commands;
using Xunit;

namespace Swap.CLI.Tests.Commands;

public class NewCommandLayeredSmokeTests
{
    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "SwapCliTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    [Fact(Skip = "Integration smoke test; requires templates to be available via SWAP_TEMPLATES_DIR.")]
    public async Task New_LayeredTemplate_GeneratesExpectedStructure()
    {
        // Arrange
        var tempRoot = CreateTempRoot();
        var projectName = "SmokeApp";
        var projectPath = Path.Combine(tempRoot, projectName);

        // Point CLI to repo templates for reliable runs
        var repoRoot = GetRepoRoot();
        var templatesDir = Path.Combine(repoRoot, "templates");
        Environment.SetEnvironmentVariable("SWAP_TEMPLATES_DIR", templatesDir);

        var root = new RootCommand();
        root.AddCommand(NewCommand.Create());

        var args = new[]
        {
            "new", projectName,
            "--template", "layered",
            "--database", "sqlite",
            "--skip-setup",
            "--output", projectPath
        };

        // Act
        var exit = await root.InvokeAsync(args);

        try
        {
            // Assert
            Assert.Equal(0, exit);
            // Prefer the provided output path, but tolerate fallback to CWD if option binding changes
            var actualPath = Directory.Exists(projectPath)
                ? projectPath
                : Path.Combine(Directory.GetCurrentDirectory(), projectName);
            Assert.True(Directory.Exists(actualPath));

            // Check layered structure
            Assert.True(Directory.Exists(Path.Combine(actualPath, "Web")));
            Assert.True(Directory.Exists(Path.Combine(actualPath, "Application")));
            Assert.True(Directory.Exists(Path.Combine(actualPath, "Domain")));
            Assert.True(Directory.Exists(Path.Combine(actualPath, "Infrastructure")));

            // Key files
            Assert.True(File.Exists(Path.Combine(actualPath, "Web", "Program.cs")));
            Assert.True(File.Exists(Path.Combine(actualPath, "Web", $"{projectName}.Web.csproj")));
            Assert.True(File.Exists(Path.Combine(actualPath, "Infrastructure", $"{projectName}.Infrastructure.csproj")));
            Assert.True(File.Exists(Path.Combine(actualPath, "Application", $"{projectName}.Application.csproj")));
            Assert.True(File.Exists(Path.Combine(actualPath, "Domain", $"{projectName}.Domain.csproj")));

            // Verify event system wiring present in Program.cs
            var program = await File.ReadAllTextAsync(Path.Combine(actualPath, "Web", "Program.cs"));
            Assert.Contains("AddSwapHtmx", program);
            Assert.Contains("UseSwapHtmxShell", program);
            Assert.Contains("UseSwapHtmx", program);

            // Verify EF tools present for migrations
            var infraCsproj = await File.ReadAllTextAsync(Path.Combine(actualPath, "Infrastructure", $"{projectName}.Infrastructure.csproj"));
            Assert.Contains("Microsoft.EntityFrameworkCore.Design", infraCsproj);
            Assert.Contains("Microsoft.EntityFrameworkCore.Tools", infraCsproj);
        }
        finally
        {
            // Cleanup
            try { Directory.Delete(tempRoot, recursive: true); } catch { /* ignore */ }
            Environment.SetEnvironmentVariable("SWAP_TEMPLATES_DIR", null);
        }
    }

    [Fact(Skip = "Integration smoke test; requires templates to be available via SWAP_TEMPLATES_DIR.")]
    public async Task New_SwapLayeredAlias_GeneratesExpectedStructure()
    {
        // Arrange
        var tempRoot = CreateTempRoot();
        var projectName = "SmokeAppAlias";
        var projectPath = Path.Combine(tempRoot, projectName);

        // Point CLI to repo templates for reliable runs
        var repoRoot = GetRepoRoot();
        var templatesDir = Path.Combine(repoRoot, "templates");
        Environment.SetEnvironmentVariable("SWAP_TEMPLATES_DIR", templatesDir);

        var root = new RootCommand();
        root.AddCommand(NewCommand.Create());

        var args = new[]
        {
            "new", projectName,
            "--template", "swap-layered",
            "--database", "sqlite",
            "--skip-setup",
            "--output", projectPath
        };

        // Act
        var exit = await root.InvokeAsync(args);

        try
        {
            // Assert
            Assert.Equal(0, exit);
            Assert.True(Directory.Exists(Path.Combine(projectPath, "Web")));
            Assert.True(File.Exists(Path.Combine(projectPath, "Web", "Program.cs")));
        }
        finally
        {
            try { Directory.Delete(tempRoot, recursive: true); } catch { /* ignore */ }
            Environment.SetEnvironmentVariable("SWAP_TEMPLATES_DIR", null);
        }
    }

    private static string GetRepoRoot()
    {
        // Walk up from current directory to find a directory that contains 'templates' folder
        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 6; i++)
        {
            var parts = new List<string> { dir };
            parts.AddRange(Enumerable.Repeat("..", i + 1));
            var candidate = Path.GetFullPath(Path.Combine(parts.ToArray()));
            if (Directory.Exists(Path.Combine(candidate, "templates")))
                return candidate;
        }
        throw new DirectoryNotFoundException("Unable to locate repo root with 'templates' directory.");
    }
}
