using System.CommandLine;
using System.CommandLine.Invocation;
using Spectre.Console;
using Swap.CLI.Infrastructure;

namespace Swap.CLI.Commands;

public static class GenerateModelCommand
{
    public static Command Create()
    {
        var command = new Command("model", "Generate an entity model with custom fields");
        command.AddAlias("m");
        
        var nameArg = new Argument<string>("name", "The name of the entity (e.g., Customer, Product)");
        var fieldsOption = new Option<string?>(
            aliases: new[] { "--fields", "-f" },
            description: "Space- or comma-separated field definitions (e.g., Name:string Email:string Age:int)");
        var dryRunOption = new Option<bool>(
            "--dry-run",
            description: "Preview what would be generated without writing files");
        var forceOption = new Option<bool>(
            "--force",
            description: "Overwrite existing files without prompting");
        var projectOption = new Option<string?>(
            aliases: new[] { "--project", "-p" },
            description: "Path to project directory (default: current directory)");
        
        command.AddArgument(nameArg);
        command.AddOption(fieldsOption);
        command.AddOption(dryRunOption);
        command.AddOption(forceOption);
        command.AddOption(projectOption);
        
        command.SetHandler(async (InvocationContext context) =>
        {
            var name = context.ParseResult.GetValueForArgument(nameArg);
            var fields = context.ParseResult.GetValueForOption(fieldsOption);
            var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            var force = context.ParseResult.GetValueForOption(forceOption);
            var project = context.ParseResult.GetValueForOption(projectOption);
            context.ExitCode = await ExecuteAsync(name, fields, dryRun, force, project);
        });
        
        return command;
    }
    
    private static async Task<int> ExecuteAsync(string entityName, string? fieldsSpec, bool dryRun, bool force, string? projectPath)
    {
        // Resolve project directory
        var workingDir = string.IsNullOrWhiteSpace(projectPath) 
            ? Directory.GetCurrentDirectory() 
            : Path.GetFullPath(projectPath);
        
        if (!Directory.Exists(workingDir))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Project directory not found: {workingDir}");
            return 1;
        }
        
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
        var projectFiles = Directory.GetFiles(workingDir, "*.csproj");
        if (projectFiles.Length == 0)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] No .csproj file found in {workingDir}. Run this command from your project root.");
            return 1;
        }
        
        var projectFile = projectFiles[0];
        var projectName = Path.GetFileNameWithoutExtension(projectFile);
        
        // Parse fields
        List<FieldDefinition> fields;
        try
        {
            fields = ParseFields(fieldsSpec);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
        
        AnsiConsole.MarkupLine($"[bold cyan]{(dryRun ? "Preview" : "Generating")} entity model:[/] {entityName}");
        AnsiConsole.MarkupLine($"[dim]Project:[/] {projectName}");
        if (fields.Any())
        {
            AnsiConsole.MarkupLine($"[dim]Fields:[/] {fields.Count}");
            foreach (var field in fields)
            {
                AnsiConsole.MarkupLine($"  [dim]•[/] {field.Name}: {field.Type}{(field.IsNullable ? "?" : "")}{(field.IsRequired ? " (required)" : "")}");
            }
        }
        else
        {
            AnsiConsole.MarkupLine($"[dim]Using default fields:[/] Id, Title, IsComplete");
        }
        AnsiConsole.WriteLine();
        
        try
        {
            // Handle dry-run mode
            if (dryRun)
            {
                var previewContent = fields.Any() 
                    ? GenerateCustomFieldsModel(entityName, projectName, fields)
                    : await GetDefaultModelContent(entityName, projectName);
                
                var panel = new Panel(previewContent)
                {
                    Header = new PanelHeader($"[yellow]Preview:[/] Models/{entityName}.cs"),
                    Border = BoxBorder.Rounded,
                    BorderStyle = new Style(Color.Yellow)
                };
                
                AnsiConsole.Write(panel);
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[yellow]ℹ[/] Dry-run mode - no files were modified");
                return 0;
            }
            
            await GenerateModelAsync(entityName, projectName, fields, force);
            
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[green]✓[/] Model generated successfully!");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Generated file:[/]");
            AnsiConsole.MarkupLine($"  Models/{entityName}.cs");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Next steps:[/]");
            AnsiConsole.MarkupLine($"  dotnet ef migrations add Add{entityName}");
            AnsiConsole.MarkupLine("  dotnet ef database update");
            
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }
    
    private static List<FieldDefinition> ParseFields(string? fieldsSpec)
    {
        return FieldHelper.ParseFields(fieldsSpec);
    }
    
    private static async Task<string> GetDefaultModelContent(string entityName, string projectName)
    {
        var templatePath = Path.Combine(AppContext.BaseDirectory, "templates", "generate", "model");
        var templateFile = Path.Combine(templatePath, "Entity.cs.template");
        var templateContent = await File.ReadAllTextAsync(templateFile);
        
        var variables = new Dictionary<string, string>
        {
            { "EntityName", entityName },
            { "ProjectName", projectName }
        };
        
        return TemplateEngine.Process(templateContent, variables);
    }
    
    private static string MapToCSharpType(string typeName)
    {
        // This method is now deprecated - FieldHelper handles type mapping
        // Keeping for backward compatibility
        return typeName.ToLower() switch
        {
            "string" or "str" => "string",
            "int" or "integer" => "int",
            "long" => "long",
            "bool" or "boolean" => "bool",
            "decimal" or "dec" => "decimal",
            "double" => "double",
            "float" => "float",
            "datetime" or "date" => "DateTime",
            "guid" or "uuid" => "Guid",
            "byte" => "byte",
            "short" => "short",
            _ => throw new ArgumentException($"Unsupported type: '{typeName}'. Supported types: string, int, long, bool, decimal, double, float, datetime, guid, byte, short")
        };
    }
    
    private static async Task GenerateModelAsync(string entityName, string projectName, List<FieldDefinition> fields, bool force)
    {
        var templatePath = Path.Combine(AppContext.BaseDirectory, "templates", "generate", "model");
        
        if (!Directory.Exists(templatePath))
        {
            throw new DirectoryNotFoundException($"Template directory not found: {templatePath}");
        }
        
        // Setup template variables
        var variables = new Dictionary<string, string>
        {
            { "EntityName", entityName },
            { "ProjectName", projectName }
        };
        
        await AnsiConsole.Status()
            .StartAsync("Generating model...", async ctx =>
            {
                string modelContent;
                
                if (fields.Any())
                {
                    // Generate custom fields model
                    modelContent = GenerateCustomFieldsModel(entityName, projectName, fields);
                }
                else
                {
                    // Use default template (Id, Title, IsComplete)
                    var templateFile = Path.Combine(templatePath, "Entity.cs.template");
                    var templateContent = await File.ReadAllTextAsync(templateFile);
                    modelContent = TemplateEngine.Process(templateContent, variables);
                }
                
                // Check if model file already exists
                Directory.CreateDirectory("Models");
                var modelFile = Path.Combine("Models", $"{entityName}.cs");
                
                if (File.Exists(modelFile) && !force)
                {
                    var overwrite = AnsiConsole.Confirm($"[yellow]File already exists:[/] {modelFile}\nOverwrite?", false);
                    if (!overwrite)
                    {
                        throw new OperationCanceledException("File generation cancelled by user");
                    }
                }
                
                // Write model file
                await File.WriteAllTextAsync(modelFile, modelContent);
                
                // Update DbContext
                ctx.Status("Updating DbContext...");
                await UpdateDbContextAsync(entityName, projectName);
            });
    }
    
    private static string GenerateCustomFieldsModel(string entityName, string projectName, List<FieldDefinition> fields)
    {
        var sb = new System.Text.StringBuilder();
        
        sb.AppendLine($"namespace {projectName}.Models;");
        sb.AppendLine();
        sb.AppendLine($"public class {entityName}");
        sb.AppendLine("{");
        
        // Always add Id property first
        sb.AppendLine("    public int Id { get; set; }");
        
        // Add custom fields
        foreach (var field in fields)
        {
            var propertyType = field.Type;
            
            // Add ? for nullable types (both value types and reference types)
            if (field.IsNullable)
            {
                propertyType += "?";
            }
            
            if (field.IsRequired)
            {
                sb.AppendLine($"    public required {propertyType} {field.Name} {{ get; set; }}");
            }
            else
            {
                sb.AppendLine($"    public {propertyType} {field.Name} {{ get; set; }}");
            }
        }
        
        sb.AppendLine("}");
        
        return sb.ToString();
    }
    
    private static bool IsValueType(string typeName)
    {
        return typeName switch
        {
            "int" or "long" or "short" or "byte" or
            "bool" or "decimal" or "double" or "float" or
            "DateTime" or "Guid" => true,
            _ => false
        };
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
        if (content.Contains($"DbSet<{entityName}>") || content.Contains($"DbSet<{projectName}.Models.{entityName}>"))
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
        
        // Find the end of this DbSet property line (look for ; or })
        var searchStart = lastDbSetIndex;
        var lineEnd = -1;
        
        // Look for either ; or } followed by newline
        for (int i = searchStart; i < content.Length; i++)
        {
            if ((content[i] == ';' || content[i] == '}') && i + 1 < content.Length)
            {
                // Found end of property, now find the newline
                lineEnd = content.IndexOf('\n', i);
                if (lineEnd != -1)
                {
                    break;
                }
            }
        }
        
        if (lineEnd == -1) lineEnd = content.Length;
        
        // Insert new DbSet property with fully qualified type name to avoid conflicts
        var newDbSet = $"\n    public DbSet<{projectName}.Models.{entityName}> {entityName}s {{ get; set; }}";
        content = content.Insert(lineEnd + 1, newDbSet);
        
        await File.WriteAllTextAsync(dbContextPath, content);
    }
}


