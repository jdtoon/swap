using System.CommandLine;
using System.CommandLine.Invocation;
using Spectre.Console;

namespace Swap.CLI.Commands;

public static class NewCommand
{
    public static Command Create()
    {
        var command = new Command("new", "Create a new Swap project");
        
        var nameArg = new Argument<string>("name", "The name of the project (e.g., MyApp)");
        var dbOption = new Option<string>("--database", () => "sqlite", "Database provider (sqlite|sqlserver|postgres)");
        var outOption = new Option<string?>("--output", "Output directory (default: ./{name})");
        
        command.AddArgument(nameArg);
        command.AddOption(dbOption);
        command.AddOption(outOption);
        
        command.SetHandler(async (InvocationContext context) =>
        {
            var name = context.ParseResult.GetValueForArgument(nameArg);
            var database = context.ParseResult.GetValueForOption(dbOption);
            var output = context.ParseResult.GetValueForOption(outOption);
            
            context.ExitCode = await ExecuteAsync(name, database!, output);
        });
        
        return command;
    }
    
    private static async Task<int> ExecuteAsync(string name, string database, string? output)
    {
        // Validate database option
        if (database != "sqlite" && database != "sqlserver" && database != "postgres")
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Invalid database option '{database}'. Must be: sqlite, sqlserver, or postgres");
            return 1;
        }
        
        var projectPath = Path.GetFullPath(output ?? name);
        
        AnsiConsole.MarkupLine($"[bold cyan]Creating new Swap project:[/] {name}");
        AnsiConsole.MarkupLine($"[dim]Database:[/] {database}");
        AnsiConsole.MarkupLine($"[dim]Location:[/] {projectPath}");
        AnsiConsole.WriteLine();
        
        // TODO: Generate project structure
        AnsiConsole.MarkupLine("[yellow]⚠ Project generation not yet implemented[/]");
        
        await Task.CompletedTask;
        return 0;
    }
}
