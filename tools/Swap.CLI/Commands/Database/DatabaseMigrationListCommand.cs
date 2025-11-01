using System.CommandLine;
using System.CommandLine.Invocation;
using Spectre.Console;

namespace Swap.CLI.Commands;

public static class DatabaseMigrationListCommand
{
    public static Command Create()
    {
        var command = new Command("list", "List all migrations");
        
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
        
        AnsiConsole.MarkupLine($"[bold cyan]Migrations:[/] {projectName}");
        AnsiConsole.WriteLine();
        
        try
        {
            string output = "";
            await AnsiConsole.Status()
                .StartAsync("Loading migrations...", async ctx =>
                {
                    output = await RunDotnetEfAsync("migrations list");
                });
            
            if (!string.IsNullOrWhiteSpace(output))
            {
                AnsiConsole.WriteLine(output);
            }
            else
            {
                AnsiConsole.MarkupLine("[dim]No migrations found.[/]");
            }
            
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message.EscapeMarkup()}");
            return 1;
        }
    }
    
    private static async Task<string> RunDotnetEfAsync(string arguments)
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
        
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        
        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"dotnet ef command failed: {error}");
        }
        
        return output;
    }
}
