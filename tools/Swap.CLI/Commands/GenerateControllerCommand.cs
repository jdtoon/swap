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
        
        var fieldsOption = new Option<string?>(
            aliases: new[] { "--fields", "-f" },
            description: "Field definitions (e.g., 'Title:string Description:string? Priority:int')");
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
            AnsiConsole.MarkupLine($"[red]Error:[/] Entity name cannot contain spaces. Use PascalCase (e.g., 'TodoItem' instead of 'Todo Item').");
            return 1;
        }
        
        if (!char.IsUpper(entityName[0]))
        {
            AnsiConsole.MarkupLine($"[yellow]Warning:[/] Entity name should start with an uppercase letter (PascalCase).");
            entityName = char.ToUpper(entityName[0]) + entityName.Substring(1);
            AnsiConsole.MarkupLine($"[dim]Using:[/] {entityName}");
        }
        
        // Parse fields
        List<FieldDefinition> fields;
        try
        {
            fields = FieldHelper.ParseFields(fieldsSpec);
        }
        catch (ArgumentException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
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
        if (fields.Any())
        {
            AnsiConsole.MarkupLine($"[dim]Fields:[/] {fields.Count}");
        }
        AnsiConsole.WriteLine();
        
        try
        {
            await GenerateControllerAsync(entityName, projectName, fields);
            
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[green]✓[/] Controller generated successfully!");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Generated files:[/]");
            AnsiConsole.MarkupLine($"  Controllers/{entityName}Controller.cs");
            if (fields.Any())
            {
                AnsiConsole.MarkupLine($"  Models/{entityName}.cs");
            }
            AnsiConsole.MarkupLine($"  ViewModels/{entityName}ListViewModel.cs");
            AnsiConsole.MarkupLine($"  Views/{entityName}/Index.cshtml");
            AnsiConsole.MarkupLine($"  Views/{entityName}/_{entityName}List.cshtml");
            AnsiConsole.MarkupLine($"  Views/{entityName}/_{entityName}CreateModal.cshtml");
            AnsiConsole.MarkupLine($"  Views/{entityName}/_{entityName}EditModal.cshtml");
            AnsiConsole.MarkupLine($"  Views/{entityName}/_{entityName}Details.cshtml");
            AnsiConsole.MarkupLine($"  Views/{entityName}/_{entityName}Form.cshtml");
            AnsiConsole.MarkupLine($"  Views/Shared/_PaginationControls.cshtml");
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
    
    private static async Task GenerateControllerAsync(string entityName, string projectName, List<FieldDefinition> fields)
    {
        var templatePath = Path.Combine(AppContext.BaseDirectory, "templates", "generate", "controller");
        
        if (!Directory.Exists(templatePath))
        {
            throw new DirectoryNotFoundException($"Template directory not found: {templatePath}");
        }
        
        // Generate field-specific content
        var entityNameLower = char.ToLower(entityName[0]) + entityName.Substring(1);
        var searchLogic = FieldHelper.GenerateSearchLogic(fields);
        var sortCases = FieldHelper.GenerateSortCases(fields);
        var filterParameters = FieldHelper.GenerateFilterParameters(fields);
        var filterParameterValues = FieldHelper.GenerateFilterParameterValues(fields);
        var filterCases = FieldHelper.GenerateFilterCases(fields);
        var filterDictionary = FieldHelper.GenerateFilterDictionary(fields);
        var filterIncludes = FieldHelper.GenerateFilterIncludes(fields);
        var filterSection = FieldHelper.GenerateFilterSection(fields, entityNameLower);
        var formFields = string.Join("\n\n", fields.Select(f => FieldHelper.GenerateFormField(f)));
        var tableHeaders = string.Join("\n                    ", fields.Select(f => FieldHelper.GenerateTableHeader(f, entityNameLower)));
        var tableCells = string.Join("\n                        ", fields.Select(f => FieldHelper.GenerateTableCell(f)));
        var detailsFields = string.Join("\n            ", fields.Select(f => FieldHelper.GenerateDetailsField(f)));
        var defaultInitialization = FieldHelper.GenerateDefaultInitialization(fields);
        
        // Generate bulk operations content
        var bulkSelectHeader = FieldHelper.GenerateBulkSelectHeader();
        var bulkSelectCell = FieldHelper.GenerateBulkSelectCell(entityNameLower);
        var bulkSelectionScript = FieldHelper.GenerateBulkSelectionScript(entityNameLower);
        var bulkActionsBar = FieldHelper.GenerateBulkActionsBar(entityName, entityNameLower);
        var bulkDeleteScript = FieldHelper.GenerateBulkDeleteScript(entityName, entityNameLower);
        
        // Setup template variables
        var variables = new Dictionary<string, string>
        {
            { "EntityName", entityName },
            { "EntityNamePlural", entityName + "s" }, // Simple pluralization for now
            { "EntityNameLower", entityNameLower },
            { "ProjectName", projectName },
            { "Namespace", projectName },
            { "SearchLogic", searchLogic },
            { "SortCases", sortCases },
            { "FilterParameters", filterParameters },
            { "FilterParameterValues", filterParameterValues },
            { "FilterCases", filterCases },
            { "FilterDictionary", filterDictionary },
            { "FilterIncludes", filterIncludes },
            { "FilterSection", filterSection },
            { "FormFields", formFields },
            { "TableHeaders", tableHeaders },
            { "TableCells", tableCells },
            { "DetailsFields", detailsFields },
            { "DefaultInitialization", defaultInitialization },
            { "BulkSelectHeader", bulkSelectHeader },
            { "BulkSelectCell", bulkSelectCell },
            { "BulkSelectionScript", bulkSelectionScript },
            { "BulkActionsBar", bulkActionsBar },
            { "BulkDeleteScript", bulkDeleteScript }
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
                
                // Generate ViewModel
                await GenerateFileFromTemplateAsync(
                    Path.Combine(templatePath, "EntityListViewModel.cs.template"),
                    Path.Combine("ViewModels", $"{entityName}ListViewModel.cs"),
                    variables,
                    ctx
                );
                
                // Generate Model (if fields specified)
                if (fields.Any())
                {
                    var modelContent = GenerateModelFromFields(entityName, projectName, fields);
                    var modelPath = Path.Combine("Models", $"{entityName}.cs");
                    Directory.CreateDirectory(Path.GetDirectoryName(modelPath) ?? "Models");
                    await File.WriteAllTextAsync(modelPath, modelContent);
                }
                
                // Generate Views
                Directory.CreateDirectory(Path.Combine("Views", entityName));
                Directory.CreateDirectory(Path.Combine("Views", "Shared"));
                
                await GenerateFileFromTemplateAsync(
                    Path.Combine(templatePath, "Views", "Index.cshtml.template"),
                    Path.Combine("Views", entityName, "Index.cshtml"),
                    variables,
                    ctx
                );
                
                await GenerateFileFromTemplateAsync(
                    Path.Combine(templatePath, "Views", "_EntityList.cshtml.template"),
                    Path.Combine("Views", entityName, $"_{entityName}List.cshtml"),
                    variables,
                    ctx
                );
                
                await GenerateFileFromTemplateAsync(
                    Path.Combine(templatePath, "Views", "_EntityCreateModal.cshtml.template"),
                    Path.Combine("Views", entityName, $"_{entityName}CreateModal.cshtml"),
                    variables,
                    ctx
                );
                
                await GenerateFileFromTemplateAsync(
                    Path.Combine(templatePath, "Views", "_EntityEditModal.cshtml.template"),
                    Path.Combine("Views", entityName, $"_{entityName}EditModal.cshtml"),
                    variables,
                    ctx
                );
                
                await GenerateFileFromTemplateAsync(
                    Path.Combine(templatePath, "Views", "_EntityDetails.cshtml.template"),
                    Path.Combine("Views", entityName, $"_{entityName}Details.cshtml"),
                    variables,
                    ctx
                );
                
                await GenerateFileFromTemplateAsync(
                    Path.Combine(templatePath, "Views", "_EntityForm.cshtml.template"),
                    Path.Combine("Views", entityName, $"_{entityName}Form.cshtml"),
                    variables,
                    ctx
                );
                
                // Generate shared pagination controls (only once)
                var paginationPath = Path.Combine("Views", "Shared", "_PaginationControls.cshtml");
                if (!File.Exists(paginationPath))
                {
                    await GenerateFileFromTemplateAsync(
                        Path.Combine(templatePath, "Views", "_PaginationControls.cshtml.template"),
                        paginationPath,
                        variables,
                        ctx
                    );
                }
                
                // Update DbContext
                ctx.Status("Updating DbContext...");
                await UpdateDbContextAsync(entityName, projectName);
            });
    }
    
    private static string GenerateModelFromFields(string entityName, string projectName, List<FieldDefinition> fields)
    {
        var properties = new List<string>();
        
        foreach (var field in fields)
        {
            var nullability = field.IsNullable ? "?" : "";
            var required = field.IsRequired ? "[Required]" : "";
            
            if (!string.IsNullOrEmpty(required))
            {
                properties.Add($"    {required}");
            }
            
            properties.Add($"    public {field.Type}{nullability} {field.Name} {{ get; set; }}");
        }
        
        return $@"using System.ComponentModel.DataAnnotations;

namespace {projectName}.Models;

public class {entityName}
{{
    public int Id {{ get; set; }}
    
{string.Join("\n    \n", properties)}
}}
";
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
        
        // Check if DbSet already exists (check for both simple and fully qualified names)
        if (content.Contains($"DbSet<{entityName}>") || 
            content.Contains($"DbSet<{projectName}.Models.{entityName}>") ||
            content.Contains($"{entityName}s {{ get; set; }}"))
        {
            AnsiConsole.MarkupLine($"[yellow]Warning:[/] DbSet for {entityName} already exists in DbContext");
            return;
        }
        
        // Find the last DbSet property and add new one after it
        var dbSetPattern = "public DbSet<";
        var lastDbSetIndex = content.LastIndexOf(dbSetPattern);
        
        if (lastDbSetIndex == -1)
        {
            throw new InvalidOperationException("Could not find any DbSet properties in DbContext");
        }
        
        // Find the end of the DbSet property (look for ; or } then newline)
        // This handles both styles: { get; set; } and => Set<T>()
        var lineEnd = -1;
        var searchStart = lastDbSetIndex;
        for (int i = searchStart; i < content.Length; i++)
        {
            if ((content[i] == ';' || content[i] == '}') && i + 1 < content.Length)
            {
                // Found property terminator, now find the newline
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
