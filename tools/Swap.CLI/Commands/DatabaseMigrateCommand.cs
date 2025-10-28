using System.CommandLine;
using System.CommandLine.Invocation;
using Spectre.Console;

namespace Swap.CLI.Commands;

public static class DatabaseMigrateCommand
{
    public static Command Create()
    {
        var command = new Command("migrate", "Create and apply Entity Framework Core migrations");
        
        var nameArg = new Argument<string?>(
            name: "name",
            description: "Migration name (e.g., AddProduct). If omitted, applies pending migrations.",
            getDefaultValue: () => null);
        
        var applyOption = new Option<bool>(
            aliases: new[] { "--apply", "-a" },
            description: "Apply the migration after creating it");
        
        command.AddArgument(nameArg);
        command.AddOption(applyOption);
        
        command.SetHandler(async (InvocationContext context) =>
        {
            var name = context.ParseResult.GetValueForArgument(nameArg);
            var apply = context.ParseResult.GetValueForOption(applyOption);
            context.ExitCode = await ExecuteAsync(name, apply);
        });
        
        return command;
    }
    
    private static async Task<int> ExecuteAsync(string? migrationName, bool apply)
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
        
        if (string.IsNullOrWhiteSpace(migrationName))
        {
            // Apply pending migrations
            AnsiConsole.MarkupLine($"[bold cyan]Applying Migrations:[/] {projectName}");
            AnsiConsole.WriteLine();
            
            try
            {
                await AnsiConsole.Status()
                    .StartAsync("Applying migrations...", async ctx =>
                    {
                        await RunDotnetEfAsync("database update");
                    });
                
                AnsiConsole.MarkupLine("[green]✓[/] Migrations applied successfully!");
                return 0;
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
                return 1;
            }
        }
        else
        {
            // Create new migration
            AnsiConsole.MarkupLine($"[bold cyan]Creating Migration:[/] {migrationName}");
            AnsiConsole.WriteLine();
            
            try
            {
                await AnsiConsole.Status()
                    .StartAsync($"Creating migration '{migrationName}'...", async ctx =>
                    {
                        await RunDotnetEfAsync($"migrations add {migrationName}");
                    });
                
                AnsiConsole.MarkupLine($"[green]✓[/] Migration '{migrationName}' created successfully!");
                
                if (apply)
                {
                    AnsiConsole.WriteLine();
                    await AnsiConsole.Status()
                        .StartAsync("Applying migration...", async ctx =>
                        {
                            await RunDotnetEfAsync("database update");
                        });
                    
                    AnsiConsole.MarkupLine("[green]✓[/] Migration applied successfully!");
                }
                else
                {
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("[dim]To apply this migration, run:[/]");
                    AnsiConsole.MarkupLine("  swap db migrate");
                    AnsiConsole.MarkupLine("  [dim]or[/] dotnet ef database update");
                }
                
                return 0;
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
                AnsiConsole.MarkupLine("[dim]Make sure you have dotnet-ef installed:[/]");
                AnsiConsole.MarkupLine("  dotnet tool install --global dotnet-ef");
                return 1;
            }
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
        
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        
        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"dotnet ef command failed: {error}");
        }
        
        // Show output for informational purposes
        if (!string.IsNullOrWhiteSpace(output))
        {
            AnsiConsole.WriteLine(output.Trim());
        }
    }
}
