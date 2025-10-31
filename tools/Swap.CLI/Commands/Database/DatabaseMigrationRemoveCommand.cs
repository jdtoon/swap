using System.CommandLine;
using System.CommandLine.Invocation;
using Spectre.Console;

namespace Swap.CLI.Commands;

public static class DatabaseMigrationRemoveCommand
{
    public static Command Create()
    {
        var command = new Command("remove", "Remove the last migration");
        
        var forceOption = new Option<bool>(
            aliases: new[] { "--force", "-f" },
            description: "Force removal even if the migration has been applied");
        
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
        
        var projectName = Path.GetFileNameWithoutExtension(projectFiles[0]);
        
        AnsiConsole.MarkupLine($"[bold cyan]Removing Last Migration:[/] {projectName}");
        AnsiConsole.WriteLine();
        
        if (!force)
        {
            var confirm = AnsiConsole.Confirm("[yellow]⚠️  Remove the last migration?[/]", false);
            if (!confirm)
            {
                AnsiConsole.MarkupLine("[dim]Operation cancelled.[/]");
                return 0;
            }
        }
        
        try
        {
            await AnsiConsole.Status()
                .StartAsync("Removing migration...", async ctx =>
                {
                    var args = force ? "migrations remove --force" : "migrations remove";
                    await RunDotnetEfAsync(args);
                });
            
            AnsiConsole.MarkupLine("[green]✓[/] Migration removed successfully!");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message.EscapeMarkup()}");
            AnsiConsole.MarkupLine("[dim]Note: You cannot remove a migration that has been applied to the database.[/]");
            AnsiConsole.MarkupLine("[dim]Use --force to remove it anyway (not recommended).[/]");
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
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
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
