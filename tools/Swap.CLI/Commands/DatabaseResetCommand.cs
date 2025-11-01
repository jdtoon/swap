using System.CommandLine;
using System.CommandLine.Invocation;
using Spectre.Console;

namespace Swap.CLI.Commands;

public static class DatabaseResetCommand
{
    public static Command Create()
    {
        var command = new Command("reset", "Drop and recreate the database");
        
        var forceOption = new Option<bool>(
            aliases: new[] { "--force", "-f" },
            description: "Skip confirmation prompt");
        
        command.AddOption(forceOption);
        
        command.SetHandler(async (InvocationContext context) =>
        {
            var force = context.ParseResult.GetValueForOption(forceOption);
            context.ExitCode = await ExecuteAsync(force);
        });
        
        return command;
    }
    
    private static async Task<int> ExecuteAsync(bool force)
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
        
        AnsiConsole.MarkupLine($"[bold cyan]Database Reset:[/] {projectName}");
        AnsiConsole.WriteLine();
        
        // Confirmation prompt
        if (!force)
        {
            var confirm = AnsiConsole.Confirm(
                "[yellow]⚠️  This will DROP the database and ALL DATA will be lost. Continue?[/]",
                false);
            
            if (!confirm)
            {
                AnsiConsole.MarkupLine("[dim]Operation cancelled.[/]");
                return 0;
            }
        }
        
        try
        {
            // Drop database
            AnsiConsole.MarkupLine("[yellow]Dropping database...[/]");
            await RunDotnetEfAsync("database drop --force");
            AnsiConsole.MarkupLine("[green]✓[/] Database dropped");
            AnsiConsole.WriteLine();
            
            // Apply migrations (recreate)
            AnsiConsole.MarkupLine("[yellow]Applying migrations...[/]");
            await RunDotnetEfAsync("database update");
            AnsiConsole.MarkupLine("[green]✓[/] Migrations applied");

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[green]✓ Database reset complete![/]");
            AnsiConsole.MarkupLine("[dim]To seed data, run: swap db seed[/]");
            
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message.EscapeMarkup()}");
            AnsiConsole.MarkupLine("[dim]Make sure you have dotnet-ef installed:[/]");
            AnsiConsole.MarkupLine("  dotnet tool install --global dotnet-ef");
            return 1;
        }
    }
    
    private static async Task RunDotnetEfAsync(string arguments)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"ef {arguments}",
            WorkingDirectory = Directory.GetCurrentDirectory(),
            RedirectStandardOutput = false,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = false
        };
        
        using var process = System.Diagnostics.Process.Start(psi);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start dotnet ef");
        }
        
        await process.WaitForExitAsync();
        
        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"dotnet ef command failed: {error}");
        }
    }
}
