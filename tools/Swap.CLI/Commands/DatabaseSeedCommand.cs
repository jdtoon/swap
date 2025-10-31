using System.CommandLine;
using System.CommandLine.Invocation;
using Spectre.Console;

namespace Swap.CLI.Commands;

public static class DatabaseSeedCommand
{
    public static Command Create()
    {
        var command = new Command("seed", "Run database seeders");
        
        var countOption = new Option<int?>(
            aliases: new[] { "--count", "-c" },
            description: "Number of records to seed (overrides SEED_COUNT env var)");
        
        var localeOption = new Option<string?>(
            aliases: new[] { "--locale", "-l" },
            description: "Bogus locale (overrides SEED_LOCALE env var)");
        
        var ifEmptyOption = new Option<bool>(
            "--if-empty",
            description: "Only seed when tables are empty (overrides SEED_IFEMPTY env var)");
        
        command.AddOption(countOption);
        command.AddOption(localeOption);
        command.AddOption(ifEmptyOption);
        
        command.SetHandler(async (InvocationContext context) =>
        {
            var count = context.ParseResult.GetValueForOption(countOption);
            var locale = context.ParseResult.GetValueForOption(localeOption);
            var ifEmpty = context.ParseResult.GetValueForOption(ifEmptyOption);
            context.ExitCode = await ExecuteAsync(count, locale, ifEmpty);
        });
        
        return command;
    }
    
    private static async Task<int> ExecuteAsync(int? count, string? locale, bool ifEmpty)
    {
        // Check if we're in a project directory
        var projectFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.csproj");
        if (projectFiles.Length == 0)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] No .csproj file found. Run this command from your project root.");
            return 1;
        }
        
        var projectFile = projectFiles[0];
        var projectName = Path.GetFileNameWithoutExtension(projectFile);
        
        // Check if SeedRunner exists
        var seedRunnerPath = Path.Combine("Data", "Seeders", "SeedRunner.cs");
        if (!File.Exists(seedRunnerPath))
        {
            AnsiConsole.MarkupLine("[yellow]Warning:[/] No seeders found.");
            AnsiConsole.MarkupLine("[dim]Generate seeders with:[/] swap g seed <entity>");
            return 1;
        }
        
        AnsiConsole.MarkupLine($"[bold cyan]Running Seeders:[/] {projectName}");
        AnsiConsole.WriteLine();
        
        // Display parameters
        var seedCount = count ?? 50;
        var seedLocale = locale ?? "en";
        
        AnsiConsole.MarkupLine($"[dim]Count:[/] {seedCount}");
        AnsiConsole.MarkupLine($"[dim]Locale:[/] {seedLocale}");
        if (ifEmpty)
        {
            AnsiConsole.MarkupLine($"[dim]If Empty:[/] true");
        }
        
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]⚠️  Note:[/] This command currently requires starting the application.");
        AnsiConsole.MarkupLine("[dim]The seeders will run, then you should stop the app (Ctrl+C).[/]");
        AnsiConsole.MarkupLine("[dim]A dedicated seeder runner will be added in a future version.[/]");
        AnsiConsole.WriteLine();
        
        // Build environment variables
        var envVars = new Dictionary<string, string>
        {
            ["SEED_COUNT"] = seedCount.ToString(),
            ["SEED_LOCALE"] = seedLocale,
            ["ASPNETCORE_ENVIRONMENT"] = "Development"
        };
        
        if (ifEmpty)
        {
            envVars["SEED_IFEMPTY"] = "true";
        }
        
        try
        {
            AnsiConsole.MarkupLine("[cyan]Starting application for seeding...[/]");
            await RunDotnetAsync("run", envVars);
            
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message.EscapeMarkup()}");
            AnsiConsole.MarkupLine("[dim]Make sure no other instance is running on the same port.[/]");
            return 1;
        }
    }
    
    private static async Task RunDotnetAsync(string arguments, Dictionary<string, string> envVars)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            WorkingDirectory = Directory.GetCurrentDirectory(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        // Add environment variables
        foreach (var (key, value) in envVars)
        {
            psi.EnvironmentVariables[key] = value;
        }
        
        using var process = System.Diagnostics.Process.Start(psi);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start dotnet run");
        }
        
        await process.WaitForExitAsync();
        
        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"dotnet run failed: {error}");
        }
    }
}
