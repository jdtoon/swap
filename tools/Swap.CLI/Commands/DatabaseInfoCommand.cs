using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.RegularExpressions;
using Spectre.Console;

namespace Swap.CLI.Commands;

public static class DatabaseInfoCommand
{
    public static Command Create()
    {
        var command = new Command("info", "Display database configuration and status");
        
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
        
        var projectFile = projectFiles[0];
        var projectName = Path.GetFileNameWithoutExtension(projectFile);
        
        AnsiConsole.MarkupLine($"[bold cyan]Database Info:[/] {projectName}");
        AnsiConsole.WriteLine();
        
        try
        {
            // Read appsettings.json for connection string
            var appSettingsPath = Path.Combine("appsettings.json");
            string? provider = null;
            string? connectionString = null;
            
            if (File.Exists(appSettingsPath))
            {
                var appSettings = await File.ReadAllTextAsync(appSettingsPath);
                
                // Extract connection string (simple regex for demo)
                var connMatch = Regex.Match(appSettings, @"""DefaultConnection""\s*:\s*""([^""]+)""");
                if (connMatch.Success)
                {
                    connectionString = connMatch.Groups[1].Value;
                    
                    // Detect provider
                    if (connectionString.Contains("Data Source=") && connectionString.EndsWith(".db"))
                        provider = "SQLite";
                    else if (connectionString.Contains("Server=") && connectionString.Contains("Database="))
                        provider = connectionString.Contains("Host=") ? "PostgreSQL" : "SQL Server";
                }
            }
            
            // Get migrations info
            int? migrationCount = null;
            string? lastMigration = null;
            
            var migrationsDir = Path.Combine("Migrations");
            if (Directory.Exists(migrationsDir))
            {
                var migrationFiles = Directory.GetFiles(migrationsDir, "*_*.cs")
                    .Where(f => !f.EndsWith(".Designer.cs"))
                    .OrderBy(f => f)
                    .ToList();
                
                migrationCount = migrationFiles.Count;
                if (migrationFiles.Any())
                {
                    var lastFile = Path.GetFileNameWithoutExtension(migrationFiles.Last());
                    var parts = lastFile.Split('_', 2);
                    lastMigration = parts.Length > 1 ? parts[1] : lastFile;
                }
            }
            
            // Display info
            var table = new Table();
            table.Border(TableBorder.Rounded);
            table.AddColumn("[bold]Property[/]");
            table.AddColumn("[bold]Value[/]");
            
            table.AddRow("Project", projectName);
            table.AddRow("Provider", provider ?? "[dim]Unknown[/]");
            table.AddRow("Connection", connectionString != null ? MaskConnectionString(connectionString) : "[dim]Not found[/]");
            table.AddRow("Migrations", migrationCount.HasValue ? migrationCount.Value.ToString() : "[dim]0[/]");
            table.AddRow("Last Migration", lastMigration ?? "[dim]None[/]");
            
            // Check for seeders
            var seedRunnerPath = Path.Combine("Data", "Seeders", "SeedRunner.cs");
            var hasSeeder = File.Exists(seedRunnerPath);
            table.AddRow("Seeders", hasSeeder ? "[green]Configured[/]" : "[dim]Not configured[/]");
            
            AnsiConsole.Write(table);
            
            // Show pending migrations (requires dotnet ef)
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Checking for pending migrations...[/]");
            
            try
            {
                var pendingOutput = await GetPendingMigrationsAsync();
                if (!string.IsNullOrWhiteSpace(pendingOutput))
                {
                    AnsiConsole.MarkupLine("[yellow]Pending migrations found:[/]");
                    AnsiConsole.WriteLine(pendingOutput);
                    AnsiConsole.MarkupLine("[dim]Apply with: swap db migrate[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("[green]✓[/] Database is up to date");
                }
            }
            catch
            {
                AnsiConsole.MarkupLine("[dim]Could not check pending migrations (dotnet-ef not installed?)[/]");
            }
            
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }
    
    private static string MaskConnectionString(string connString)
    {
        // Mask passwords in connection string
        return Regex.Replace(connString, 
            @"(Password|Pwd)\s*=\s*[^;]+", 
            "$1=****", 
            RegexOptions.IgnoreCase);
    }
    
    private static async Task<string> GetPendingMigrationsAsync()
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "ef migrations list",
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
            throw new InvalidOperationException("dotnet ef migrations list failed");
        }
        
        return output;
    }
}
