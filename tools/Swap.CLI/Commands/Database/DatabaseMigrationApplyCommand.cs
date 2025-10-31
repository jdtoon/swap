using System.CommandLine;
using System.CommandLine.Invocation;
using Spectre.Console;

namespace Swap.CLI.Commands;

public static class DatabaseMigrationApplyCommand
{
    public static Command Create()
    {
        var command = new Command("apply", "Apply pending migrations to the database");
        
        command.SetHandler(async (InvocationContext context) =>
        {
            context.ExitCode = await ExecuteAsync();
        });
        
        return command;
    }
    
    private static async Task<int> ExecuteAsync()
    {
        // Check if we're in a project directory
        var projectFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.csproj");
        if (projectFiles.Length == 0)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] No .csproj file found. Run this command from your project root.");
            return 1;
        }
        
        var projectName = Path.GetFileNameWithoutExtension(projectFiles[0]);
        
        AnsiConsole.MarkupLine($"[bold cyan]Applying Migrations:[/] {projectName}");
        AnsiConsole.WriteLine();
        
        try
        {
            string output = "";
            await AnsiConsole.Status()
                .StartAsync("Applying migrations...", async ctx =>
                {
                    output = await RunDotnetEfWithOutputAsync("database update");
                });
            
            AnsiConsole.MarkupLine("[green]✓[/] Migrations applied successfully!");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message.EscapeMarkup()}");
            AnsiConsole.MarkupLine("[dim]Make sure you have dotnet-ef installed:[/]");
            AnsiConsole.MarkupLine("  [cyan]dotnet tool install --global dotnet-ef[/]");
            return 1;
        }
    }
    
    private static async Task<string> RunDotnetEfWithOutputAsync(string arguments)
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
        
        return "";
    }
}
