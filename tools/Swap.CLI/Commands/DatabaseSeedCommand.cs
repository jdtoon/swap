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
        
        // Build environment variables
        var envVars = new Dictionary<string, string>();
        if (count.HasValue)
        {
            envVars["SEED_COUNT"] = count.Value.ToString();
            AnsiConsole.MarkupLine($"[dim]Count:[/] {count.Value}");
        }
        if (!string.IsNullOrWhiteSpace(locale))
        {
            envVars["SEED_LOCALE"] = locale;
            AnsiConsole.MarkupLine($"[dim]Locale:[/] {locale}");
        }
        if (ifEmpty)
        {
            envVars["SEED_IFEMPTY"] = "true";
            AnsiConsole.MarkupLine($"[dim]If Empty:[/] true");
        }
        
        AnsiConsole.WriteLine();
        
        try
        {
            await AnsiConsole.Status()
                .StartAsync("Running seeders...", async ctx =>
                {
                    await RunDotnetAsync("run", envVars);
                });
            
            AnsiConsole.MarkupLine("[green]✓[/] Seeding complete!");
            AnsiConsole.MarkupLine("[dim]Note: The application will start and seed data, then you can stop it (Ctrl+C).[/]");
            
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message.EscapeMarkup()}");
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
