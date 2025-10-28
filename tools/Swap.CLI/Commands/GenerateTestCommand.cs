using System.CommandLine;
using System.CommandLine.Invocation;
using Spectre.Console;
using Swap.CLI.Infrastructure;

namespace Swap.CLI.Commands;

public static class GenerateTestCommand
{
    public static Command Create()
    {
        var command = new Command("test", "Generate integration test class for a controller");
        command.AddAlias("t");

        var nameArg = new Argument<string>(
            name: "controller",
            description: "Controller name (e.g., TodoItem or TodoItemController)");

        var forceOption = new Option<bool>(
            aliases: new[] { "--force", "-f" },
            description: "Overwrite existing test file without prompting");

        var projectOption = new Option<string?>(
            aliases: new[] { "--project", "-p" },
            description: "Path to project directory (default: current directory)");

        var outputOption = new Option<string?>(
            aliases: new[] { "--output", "-o" },
            description: "Output directory for test file (default: Tests/)");

        command.AddArgument(nameArg);
        command.AddOption(forceOption);
        command.AddOption(projectOption);
        command.AddOption(outputOption);

        command.SetHandler(async (InvocationContext ctx) =>
        {
            var controller = ctx.ParseResult.GetValueForArgument(nameArg);
            var force = ctx.ParseResult.GetValueForOption(forceOption);
            var projectPath = ctx.ParseResult.GetValueForOption(projectOption);
            var outputPath = ctx.ParseResult.GetValueForOption(outputOption);

            ctx.ExitCode = await ExecuteAsync(controller, force, projectPath, outputPath);
        });

        return command;
    }

    public static async Task<int> ExecuteAsync(string controller, bool force, string? projectPath, string? outputPath)
    {
        // Resolve working directory
        var workingDir = !string.IsNullOrEmpty(projectPath)
            ? Path.GetFullPath(projectPath)
            : Directory.GetCurrentDirectory();

        // Validate cwd
        var projectFiles = Directory.GetFiles(workingDir, "*.csproj");
        if (projectFiles.Length == 0)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] No .csproj file found in {workingDir}. Run from your project root.");
            return 1;
        }

        var projectFile = projectFiles[0];
        var projectName = Path.GetFileNameWithoutExtension(projectFile);

        try
        {
            // Normalize controller name
            var controllerName = controller.EndsWith("Controller") 
                ? controller 
                : controller + "Controller";
            var entityName = controllerName.Replace("Controller", "");

            // Determine base path (pluralized entity name in lowercase)
            var basePath = $"/{Pluralize(entityName.ToLower())}";

            // Ensure Swap.Testing package reference
            await EnsureSwapTestingPackageAsync(projectFile);

            // Determine output directory
            var testDir = !string.IsNullOrEmpty(outputPath)
                ? Path.Combine(workingDir, outputPath)
                : Path.Combine(workingDir, "Tests");

            Directory.CreateDirectory(testDir);

            // Generate test file
            var testFileName = $"{controllerName}Tests.cs";
            var testFilePath = Path.Combine(testDir, testFileName);

            // Check if file exists
            if (File.Exists(testFilePath) && !force)
            {
                var overwrite = AnsiConsole.Confirm($"[yellow]File {testFileName} already exists. Overwrite?[/]", false);
                if (!overwrite)
                {
                    AnsiConsole.MarkupLine("[yellow]Skipped.[/]");
                    return 0;
                }
            }

            // Load template
            var templatePath = Path.Combine(AppContext.BaseDirectory, "templates", "generate", "test", "ControllerTests.cs.template");
            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Test template not found: {templatePath}");

            var template = await File.ReadAllTextAsync(templatePath);

            // Replace placeholders
            var content = template
                .Replace("{{PROJECT_NAME}}", projectName)
                .Replace("{{CONTROLLER_NAME}}", controllerName)
                .Replace("{{BASE_PATH}}", basePath);

            // Write file
            await File.WriteAllTextAsync(testFilePath, content);

            AnsiConsole.MarkupLine($"[green]✓[/] Generated [cyan]{testFileName}[/]");
            AnsiConsole.MarkupLine($"  [dim]→ {testFilePath}[/]");
            AnsiConsole.MarkupLine("");
            AnsiConsole.MarkupLine("[yellow]Next steps:[/]");
            AnsiConsole.MarkupLine("  1. Add Swap.Testing package reference if not already present");
            AnsiConsole.MarkupLine("  2. Update TODO comments in test methods");
            AnsiConsole.MarkupLine("  3. Add test data setup/teardown as needed");
            AnsiConsole.MarkupLine("  4. Run tests: [cyan]dotnet test[/]");

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    private static async Task EnsureSwapTestingPackageAsync(string projectFile)
    {
        var content = await File.ReadAllTextAsync(projectFile);

        if (!content.Contains("Swap.Testing"))
        {
            AnsiConsole.MarkupLine("[yellow]Note:[/] Swap.Testing package not found. Add it with:");
            AnsiConsole.MarkupLine("  [cyan]dotnet add package Swap.Testing[/]");
            AnsiConsole.MarkupLine("");
        }
    }

    private static string Pluralize(string word)
    {
        // Simple pluralization rules
        if (word.EndsWith("y"))
            return word.Substring(0, word.Length - 1) + "ies";
        if (word.EndsWith("s") || word.EndsWith("x") || word.EndsWith("ch") || word.EndsWith("sh"))
            return word + "es";
        return word + "s";
    }
}
