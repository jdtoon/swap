using System.CommandLine;
using System.CommandLine.Invocation;
using Spectre.Console;
using Swap.CLI.Infrastructure;

namespace Swap.CLI.Commands;

public static class GenerateControllerCommand
{
    public static Command Create()
    {
        var command = new Command("controller", "Generate a CRUD controller with HTMX views");
        
        var nameArg = new Argument<string>("name", "The name of the entity (e.g., Task, Product)");
        command.AddArgument(nameArg);
        
        command.SetHandler(async (InvocationContext context) =>
        {
            var name = context.ParseResult.GetValueForArgument(nameArg);
            context.ExitCode = await ExecuteAsync(name);
        });
        
        return command;
    }
    
    private static async Task<int> ExecuteAsync(string entityName)
    {
        // Validate entity name
        if (string.IsNullOrWhiteSpace(entityName))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Entity name cannot be empty.");
            return 1;
        }
        
        if (entityName.Contains(' '))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Entity name cannot contain spaces. Use PascalCase (e.g., 'TodoItem' instead of 'Todo Item').");
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
        
        AnsiConsole.MarkupLine($"[bold cyan]Generating CRUD controller:[/] {entityName}");
        AnsiConsole.MarkupLine($"[dim]Project:[/] {projectName}");
        AnsiConsole.WriteLine();
        
        try
        {
            await GenerateControllerAsync(entityName, projectName);
            
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[green]✓[/] Controller generated successfully!");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Generated files:[/]");
            AnsiConsole.MarkupLine($"  Controllers/{entityName}Controller.cs");
            AnsiConsole.MarkupLine($"  Models/{entityName}.cs");
            AnsiConsole.MarkupLine($"  Views/{entityName}/Index.cshtml");
            AnsiConsole.MarkupLine($"  Views/{entityName}/_{entityName}List.cshtml");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Next steps:[/]");
            AnsiConsole.MarkupLine($"  dotnet ef migrations add Add{entityName}");
            AnsiConsole.MarkupLine("  dotnet ef database update");
            AnsiConsole.MarkupLine($"  # Navigate to /{entityName} in your browser");
            
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }
    
    private static async Task GenerateControllerAsync(string entityName, string projectName)
    {
        var templatePath = Path.Combine(AppContext.BaseDirectory, "templates", "generate", "controller");
        
        if (!Directory.Exists(templatePath))
        {
            throw new DirectoryNotFoundException($"Template directory not found: {templatePath}");
        }
        
        // Setup template variables
        var variables = new Dictionary<string, string>
        {
            { "EntityName", entityName },
            { "EntityNamePlural", entityName + "s" }, // Simple pluralization for now
            { "EntityNameLower", char.ToLower(entityName[0]) + entityName.Substring(1) },
            { "ProjectName", projectName }
        };
        
        await AnsiConsole.Status()
            .StartAsync("Generating files...", async ctx =>
            {
                // Generate Controller
                await GenerateFileFromTemplateAsync(
                    Path.Combine(templatePath, "EntityController.cs.template"),
                    Path.Combine("Controllers", $"{entityName}Controller.cs"),
                    variables,
                    ctx
                );
                
                // Generate Model
                await GenerateFileFromTemplateAsync(
                    Path.Combine(templatePath, "Entity.cs.template"),
                    Path.Combine("Models", $"{entityName}.cs"),
                    variables,
                    ctx
                );
                
                // Generate Views
                Directory.CreateDirectory(Path.Combine("Views", entityName));
                
                await GenerateFileFromTemplateAsync(
                    Path.Combine(templatePath, "Index.cshtml.template"),
                    Path.Combine("Views", entityName, "Index.cshtml"),
                    variables,
                    ctx
                );
                
                await GenerateFileFromTemplateAsync(
                    Path.Combine(templatePath, "_EntityList.cshtml.template"),
                    Path.Combine("Views", entityName, $"_{entityName}List.cshtml"),
                    variables,
                    ctx
                );
                
                // Update DbContext
                ctx.Status("Updating DbContext...");
                await UpdateDbContextAsync(entityName, projectName);
            });
    }
    
    private static async Task GenerateFileFromTemplateAsync(
        string templateFile,
        string targetFile,
        Dictionary<string, string> variables,
        StatusContext ctx)
    {
        ctx.Status($"Creating {targetFile}...");
        
        var templateContent = await File.ReadAllTextAsync(templateFile);
        var processedContent = TemplateEngine.Process(templateContent, variables);
        
        var targetDir = Path.GetDirectoryName(targetFile);
        if (!string.IsNullOrEmpty(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }
        
        await File.WriteAllTextAsync(targetFile, processedContent);
        await Task.Delay(50); // Visual feedback
    }
    
    private static async Task UpdateDbContextAsync(string entityName, string projectName)
    {
        var dbContextPath = Path.Combine("Data", "AppDbContext.cs");
        
        if (!File.Exists(dbContextPath))
        {
            throw new FileNotFoundException($"DbContext not found at {dbContextPath}");
        }
        
        var content = await File.ReadAllTextAsync(dbContextPath);
        
        // Check if DbSet already exists
        if (content.Contains($"DbSet<{entityName}>"))
        {
            AnsiConsole.MarkupLine($"[yellow]Warning:[/] DbSet<{entityName}> already exists in DbContext");
            return;
        }
        
        // Find the last DbSet property and add new one after it
        var dbSetPattern = "public DbSet<";
        var lastDbSetIndex = content.LastIndexOf(dbSetPattern);
        
        if (lastDbSetIndex == -1)
        {
            throw new InvalidOperationException("Could not find any DbSet properties in DbContext");
        }
        
        // Find the end of the line after the last DbSet
        var lineEnd = content.IndexOf('\n', lastDbSetIndex);
        if (lineEnd == -1) lineEnd = content.Length;
        
        // Insert new DbSet property with fully qualified type name to avoid conflicts
        var newDbSet = $"\n    public DbSet<{projectName}.Models.{entityName}> {entityName}s {{ get; set; }}";
        content = content.Insert(lineEnd + 1, newDbSet);
        
        await File.WriteAllTextAsync(dbContextPath, content);
    }
}
