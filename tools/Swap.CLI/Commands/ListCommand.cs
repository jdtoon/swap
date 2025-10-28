using System.CommandLine;
using System.Text.RegularExpressions;
using Spectre.Console;

namespace Swap.CLI.Commands;

public static class ListCommand
{
    public static Command Create()
    {
        var command = new Command("list", "List resources (entities) in the project");
        
        var projectOption = new Option<string?>(
            aliases: new[] { "--project", "-p" },
            description: "Path to project directory (default: current directory)");
        command.AddOption(projectOption);
        
        command.SetHandler(async (string? projectPath) =>
        {
            await ExecuteAsync(projectPath);
        }, projectOption);
        
        return command;
    }
    
    private static async Task ExecuteAsync(string? projectPath)
    {
        // Resolve working directory
        var workingDir = !string.IsNullOrEmpty(projectPath)
            ? Path.GetFullPath(projectPath)
            : Directory.GetCurrentDirectory();
        
        // Check if we're in a project directory
        var projectFiles = Directory.GetFiles(workingDir, "*.csproj");
        if (projectFiles.Length == 0)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] No .csproj file found in {workingDir}");
            return;
        }
        
        var projectFile = projectFiles[0];
        var projectName = Path.GetFileNameWithoutExtension(projectFile);
        
        // Check for DbContext
        var dbContextPath = Path.Combine(workingDir, "Data", "AppDbContext.cs");
        if (!File.Exists(dbContextPath))
        {
            AnsiConsole.MarkupLine($"[yellow]Warning:[/] No DbContext found at Data/AppDbContext.cs");
            return;
        }
        
        AnsiConsole.MarkupLine($"[bold cyan]Resources in {projectName}:[/]");
        AnsiConsole.WriteLine();
        
        try
        {
            var entities = await GetEntitiesFromDbContextAsync(dbContextPath);
            
            if (!entities.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No DbSet<T> entries found in DbContext[/]");
                return;
            }
            
            var table = new Table();
            table.Border(TableBorder.Rounded);
            table.AddColumn("[bold]Entity[/]");
            table.AddColumn("[bold]Model[/]");
            table.AddColumn("[bold]Controller[/]");
            table.AddColumn("[bold]Seeder[/]");
            
            foreach (var entity in entities)
            {
                var modelExists = File.Exists(Path.Combine(workingDir, "Models", $"{entity}.cs"));
                var controllerExists = File.Exists(Path.Combine(workingDir, "Controllers", $"{entity}Controller.cs"));
                var seederExists = File.Exists(Path.Combine(workingDir, "Data", "Seeders", $"{entity}Seeder.cs"));
                
                table.AddRow(
                    $"[cyan]{entity}[/]",
                    modelExists ? "[green]✓[/]" : "[dim]✗[/]",
                    controllerExists ? "[green]✓[/]" : "[dim]✗[/]",
                    seederExists ? "[green]✓[/]" : "[dim]✗[/]"
                );
            }
            
            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[dim]Found {entities.Count} resource(s)[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
        }
    }
    
    private static async Task<List<string>> GetEntitiesFromDbContextAsync(string dbContextPath)
    {
        var content = await File.ReadAllTextAsync(dbContextPath);
        var entities = new List<string>();
        
        // Match DbSet<EntityName> or DbSet<Namespace.EntityName> patterns
        var pattern = @"DbSet<(?:[\w\.]+\.)?(\w+)>";
        var matches = Regex.Matches(content, pattern);
        
        foreach (Match match in matches)
        {
            if (match.Groups.Count > 1)
            {
                var entityName = match.Groups[1].Value;
                if (!entities.Contains(entityName))
                {
                    entities.Add(entityName);
                }
            }
        }
        
        return entities;
    }
}
