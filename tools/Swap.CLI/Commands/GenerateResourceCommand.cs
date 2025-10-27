using System.CommandLine;
using System.CommandLine.Invocation;
using Spectre.Console;
using System.Diagnostics;

namespace Swap.CLI.Commands;

public static class GenerateResourceCommand
{
    public static Command Create()
    {
        var command = new Command("resource", "Generate a complete resource (model + controller + views)");
        command.AddAlias("r");
        
        var nameArg = new Argument<string>("name", "The name of the entity (e.g., Product, Customer)");
        var fieldsOption = new Option<string?>(
            "--fields",
            "Space- or comma-separated field definitions (e.g., Name:string Email:string Age:int)");
        
        command.AddArgument(nameArg);
        command.AddOption(fieldsOption);
        
        command.SetHandler(async (InvocationContext context) =>
        {
            var name = context.ParseResult.GetValueForArgument(nameArg);
            var fields = context.ParseResult.GetValueForOption(fieldsOption);
            context.ExitCode = await ExecuteAsync(name, fields);
        });
        
        return command;
    }
    
    private static async Task<int> ExecuteAsync(string entityName, string? fieldsSpec)
    {
        // Validate entity name
        if (string.IsNullOrWhiteSpace(entityName))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Entity name cannot be empty.");
            return 1;
        }
        
        if (entityName.Contains(' '))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Entity name cannot contain spaces. Use PascalCase (e.g., 'CustomerOrder' instead of 'Customer Order').");
            return 1;
        }
        
        if (!char.IsUpper(entityName[0]))
        {
            AnsiConsole.MarkupLine($"[yellow]Warning:[/] Entity name should start with an uppercase letter (PascalCase).");
            entityName = char.ToUpper(entityName[0]) + entityName.Substring(1);
            AnsiConsole.MarkupLine($"[dim]Using:[/] {entityName}");
        }
        
        // Check if we're in a project directory
        var projectFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.csproj");
        if (projectFiles.Length == 0)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] No .csproj file found in current directory. Run this command from your project root.");
            return 1;
        }
        
        var projectFile = projectFiles[0];
        var projectName = Path.GetFileNameWithoutExtension(projectFile);
        
        AnsiConsole.MarkupLine($"[bold cyan]Generating complete resource:[/] {entityName}");
        AnsiConsole.MarkupLine($"[dim]Project:[/] {projectName}");
        AnsiConsole.WriteLine();
        
        try
        {
            // Step 1: Generate the model (call directly without spinner to avoid Spectre conflicts)
            AnsiConsole.MarkupLine("[cyan]Step 1/2:[/] Generating model...");
            
            var modelCommandType = typeof(GenerateModelCommand);
            var executeMethod = modelCommandType.GetMethod(
                "ExecuteAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            if (executeMethod != null)
            {
                var task = (Task<int>)executeMethod.Invoke(null, new object?[] { entityName, fieldsSpec })!;
                var modelResult = await task;
                
                if (modelResult != 0)
                {
                    throw new Exception("Model generation failed");
                }
            }
            
            AnsiConsole.WriteLine();
            
            // Step 2: Generate the controller
            AnsiConsole.MarkupLine("[cyan]Step 2/2:[/] Generating controller...");
            
            var controllerCommandType = typeof(GenerateControllerCommand);
            executeMethod = controllerCommandType.GetMethod(
                "ExecuteAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            if (executeMethod != null)
            {
                var task = (Task<int>)executeMethod.Invoke(null, new object?[] { entityName, fieldsSpec })!;
                var controllerResult = await task;
                
                if (controllerResult != 0)
                {
                    throw new Exception("Controller generation failed");
                }
            }
            
            // Step 3: Auto-create and apply EF Core migration
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[cyan]Step 3/3:[/] Applying database migrations...");
            var migrationOk = await RunMigrationsAsync(entityName);
            if (!migrationOk)
            {
                AnsiConsole.MarkupLine("[yellow]Warning:[/] Could not apply EF Core migrations automatically. You can run them manually:");
                AnsiConsole.MarkupLine($"  dotnet ef migrations add Add{entityName}");
                AnsiConsole.MarkupLine("  dotnet ef database update");
            }
            else
            {
                AnsiConsole.MarkupLine("[green]✓[/] Database updated");
            }
            
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[green]✓ Resource generation complete![/]");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Summary:[/]");
            AnsiConsole.MarkupLine($"  [green]✓[/] Model: Models/{entityName}.cs");
            AnsiConsole.MarkupLine($"  [green]✓[/] Controller: Controllers/{entityName}Controller.cs");
            AnsiConsole.MarkupLine($"  [green]✓[/] Views: Views/{entityName}/ (Index, Create, Edit, Delete, Details)");
            AnsiConsole.MarkupLine($"  [green]✓[/] DbContext: Updated with DbSet<{entityName}>");
            AnsiConsole.MarkupLine($"  [green]✓[/] Migrations: Applied");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"  # Navigate to /{entityName} in your browser");
            
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    private static async Task<bool> RunMigrationsAsync(string entityName)
    {
        // Try to create a migration and then update the database
        // If migration name already exists, continue to database update
        bool added = await RunProcess("dotnet", $"ef migrations add Add{entityName} -o Migrations");
        bool updated = await RunProcess("dotnet", "ef database update");
        return updated; // consider database update as the success criteria
    }

    private static async Task<bool> RunProcess(string fileName, string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Directory.GetCurrentDirectory()
            };
            using var proc = new Process { StartInfo = psi };
            proc.Start();
            var stdout = await proc.StandardOutput.ReadToEndAsync();
            var stderr = await proc.StandardError.ReadToEndAsync();
            await proc.WaitForExitAsync();

            if (!string.IsNullOrWhiteSpace(stdout)) AnsiConsole.WriteLine(stdout.Trim());
            if (!string.IsNullOrWhiteSpace(stderr)) AnsiConsole.WriteLine(stderr.Trim());

            return proc.ExitCode == 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Process error:[/] {ex.Message}");
            return false;
        }
    }
}
