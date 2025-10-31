using System.CommandLine;
using System.CommandLine.Invocation;
using Spectre.Console;

namespace Swap.CLI.Commands;

public static class DatabaseMigrationAddCommand
{
    public static Command Create()
    {
        var command = new Command("add", "Create a new migration");
        
        var nameArg = new Argument<string>(
            name: "name",
            description: "Migration name (e.g., AddProduct)");
        
        command.AddArgument(nameArg);
        
        command.SetHandler(async (InvocationContext context) =>
        {
            var name = context.ParseResult.GetValueForArgument(nameArg);
            context.ExitCode = await ExecuteAsync(name);
        });
        
        return command;
    }
    
    private static async Task<int> ExecuteAsync(string migrationName)
    {
        // Check if we're in a project directory
        var projectFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.csproj");
        if (projectFiles.Length == 0)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] No .csproj file found. Run this command from your project root.");
            return 1;
        }
        
        var projectName = Path.GetFileNameWithoutExtension(projectFiles[0]);
        
        AnsiConsole.MarkupLine($"[bold cyan]Creating Migration:[/] {migrationName}");
        AnsiConsole.WriteLine();
        
        try
        {
            string output = "";
            await AnsiConsole.Status()
                .StartAsync($"Creating migration '{migrationName}'...", async ctx =>
                {
                    output = await RunDotnetEfAsync($"migrations add {migrationName}");
                });
            
            AnsiConsole.MarkupLine($"[green]✓[/] Migration '{migrationName}' created successfully!");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]To apply this migration, run:[/]");
            AnsiConsole.MarkupLine("  [cyan]swap db migration apply[/]");
            
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
