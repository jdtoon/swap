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
        command.AddAlias("c");
        
        var nameArg = new Argument<string>("name", "The name of the entity (e.g., Task, Product)");
        command.AddArgument(nameArg);
        
        var fieldsOption = new Option<string?>(
            aliases: new[] { "--fields", "-f" },
            description: "Field definitions (e.g., 'Title:string Description:string? Priority:int')");
        command.AddOption(fieldsOption);
        
        var dryRunOption = new Option<bool>(
            aliases: new[] { "--dry-run" },
            description: "Preview what would be generated without writing files");
        command.AddOption(dryRunOption);
        
        var forceOption = new Option<bool>(
            aliases: new[] { "--force" },
            description: "Overwrite existing files without prompting");
        command.AddOption(forceOption);
        
        var projectOption = new Option<string?>(
            aliases: new[] { "--project", "-p" },
            description: "Path to project directory (default: current directory)");
        command.AddOption(projectOption);

        var addNavOption = new Option<bool>(
            aliases: new[] { "--add-nav" },
            description: "Inject a navigation link for this controller into _Layout.cshtml (HTMX-first nav)"
        );
        command.AddOption(addNavOption);

        var noMigrationsOption = new Option<bool>(
            aliases: new[] { "--no-migrations" },
            description: "Skip automatic migration creation (you'll need to create migrations manually)"
        );
        command.AddOption(noMigrationsOption);
        
        command.SetHandler(async (InvocationContext context) =>
        {
            var name = context.ParseResult.GetValueForArgument(nameArg);
            var fields = context.ParseResult.GetValueForOption(fieldsOption);
            var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            var force = context.ParseResult.GetValueForOption(forceOption);
            var projectPath = context.ParseResult.GetValueForOption(projectOption);
            var addNav = context.ParseResult.GetValueForOption(addNavOption);
            var noMigrations = context.ParseResult.GetValueForOption(noMigrationsOption);
            context.ExitCode = await ExecuteAsync(name, fields, dryRun, force, projectPath, addNav, noMigrations);
        });
        
        return command;
    }
    
    private static async Task<int> ExecuteAsync(string entityName, string? fieldsSpec, bool dryRun, bool force, string? projectPath, bool addNav, bool noMigrations)
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
        
        // Resolve working directory
        var workingDir = !string.IsNullOrEmpty(projectPath)
            ? Path.GetFullPath(projectPath)
            : Directory.GetCurrentDirectory();
        
        // Check if we're in a project directory
        var projectFiles = Directory.GetFiles(workingDir, "*.csproj");
        if (projectFiles.Length == 0)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] No .csproj file found in {workingDir}. Run this command from your project root.");
            return 1;
        }
        
        var projectFile = projectFiles[0];
        var projectName = Path.GetFileNameWithoutExtension(projectFile);
        
        AnsiConsole.MarkupLine($"[bold cyan]{(dryRun ? "Preview" : "Generating")} CRUD controller:[/] {entityName}");
        AnsiConsole.MarkupLine($"[dim]Project:[/] {projectName}");
        if (fields.Any())
        {
            AnsiConsole.MarkupLine($"[dim]Fields:[/] {fields.Count}");
        }
        AnsiConsole.WriteLine();
        
        try
        {
            // Handle dry-run mode
            if (dryRun)
            {
                AnsiConsole.MarkupLine("[yellow]ℹ[/] Dry-run mode for controllers would generate:");
                AnsiConsole.MarkupLine($"  • Controllers/{entityName}Controller.cs");
                if (fields.Any())
                {
                    AnsiConsole.MarkupLine($"  • Models/{entityName}.cs");
                }
                AnsiConsole.MarkupLine($"  • ViewModels/{entityName}ListViewModel.cs");
                AnsiConsole.MarkupLine($"  • Views/{entityName}/Index.cshtml");
                AnsiConsole.MarkupLine($"  • Views/{entityName}/_{entityName}List.cshtml");
                AnsiConsole.MarkupLine($"  • Views/{entityName}/_{entityName}CreateModal.cshtml");
                AnsiConsole.MarkupLine($"  • Views/{entityName}/_{entityName}EditModal.cshtml");
                AnsiConsole.MarkupLine($"  • Views/{entityName}/_{entityName}Details.cshtml");
                AnsiConsole.MarkupLine($"  • Views/{entityName}/_{entityName}Form.cshtml");
                AnsiConsole.MarkupLine($"  • Views/Shared/_PaginationControls.cshtml");
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[yellow]ℹ[/] Dry-run mode - no files were modified");
                AnsiConsole.MarkupLine("[dim]Note:[/] Use [cyan]swap g m {entityName} -f \"...\" --dry-run[/] to preview individual files");
                return 0;
            }
            
            await GenerateControllerAsync(entityName, projectName, fields, force);

            // Auto-create migration for the new entity (never applies database update)
            if (!noMigrations)
            {
                await TryCreateMigrationAsync(workingDir, entityName);
            }
            else
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[yellow]ℹ[/] Migration creation skipped (--no-migrations flag)");
                AnsiConsole.MarkupLine("[dim]Create migration manually:[/] [cyan]dotnet ef migrations add Add{0}[/]", entityName);
            }

            // Optionally inject nav link
            if (addNav)
            {
                await TryInjectNavLinkAsync(workingDir, entityName);
            }
            
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
            AnsiConsole.MarkupLine("  # (We never auto-run database updates)");
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

    private static async Task TryInjectNavLinkAsync(string workingDir, string entityName)
    {
        try
        {
            var layoutPath = Path.Combine(workingDir, "Views", "Shared", "_Layout.cshtml");
            if (!File.Exists(layoutPath))
            {
                AnsiConsole.MarkupLine("[yellow]ℹ[/] _Layout.cshtml not found, skipping --add-nav");
                return;
            }

            var content = await File.ReadAllTextAsync(layoutPath);
            var linkText = entityName + "s"; // simple plural
            var li = $"<li><a href=\"/{entityName}\" hx-target=\"#main-content\" hx-push-url=\"true\">{linkText}</a></li>";

            // Prefer inserting inside the primary nav UL
            var ulIdx = content.IndexOf("<ul class=\"menu menu-horizontal", StringComparison.OrdinalIgnoreCase);
            if (ulIdx >= 0)
            {
                var ulClose = content.IndexOf("</ul>", ulIdx, StringComparison.OrdinalIgnoreCase);
                if (ulClose > ulIdx)
                {
                    content = content.Insert(ulClose, "\n                        " + li + "\n");
                }
            }
            else
            {
                // Fallback: insert before </nav>
                var navClose = content.LastIndexOf("</nav>", StringComparison.OrdinalIgnoreCase);
                if (navClose >= 0)
                {
                    var fallbackLink = $"<a href=\"/{entityName}\" hx-target=\"#main-content\" hx-push-url=\"true\" class=\"btn btn-ghost\">{linkText}</a>";
                    content = content.Insert(navClose, "\n                " + fallbackLink + "\n");
                }
                else
                {
                    // Else: insert after opening <body>
                    var bodyIdx = content.IndexOf("<body", StringComparison.OrdinalIgnoreCase);
                    if (bodyIdx >= 0)
                    {
                        var bodyEnd = content.IndexOf('>', bodyIdx);
                        if (bodyEnd > bodyIdx)
                        {
                            var fallbackLink = $"<a href=\"/{entityName}\" hx-target=\"#main-content\" hx-push-url=\"true\" class=\"btn btn-ghost\">{linkText}</a>";
                            content = content.Insert(bodyEnd + 1, "\n        <!-- Nav injected by swap --add-nav -->\n        " + fallbackLink + "\n");
                        }
                    }
                }
            }

            await File.WriteAllTextAsync(layoutPath, content);
            AnsiConsole.MarkupLine("[green]✓[/] Navigation link injected into _Layout.cshtml");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]⚠[/] Could not inject nav link: {ex.Message}");
        }
    }
    
    private static async Task TryCreateMigrationAsync(string workingDir, string entityName)
    {
        try
        {
            // Rigid gate: build before migrations
            var build = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "build",
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using (var buildProc = System.Diagnostics.Process.Start(build))
            {
                if (buildProc != null)
                {
                    await buildProc.WaitForExitAsync();
                    if (buildProc.ExitCode != 0)
                    {
                        AnsiConsole.MarkupLine("[red]✗ Build failed before migration creation[/]");
                        var err = await buildProc.StandardError.ReadToEndAsync();
                        var outp = await buildProc.StandardOutput.ReadToEndAsync();
                        if (!string.IsNullOrWhiteSpace(outp)) AnsiConsole.WriteLine(outp);
                        if (!string.IsNullOrWhiteSpace(err)) AnsiConsole.WriteLine(err);
                        return;
                    }
                }
            }

            // Detect available DbContexts
            var dbContexts = FindDbContextCandidates(workingDir);
            string? contextName = null;

            if (dbContexts.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]ℹ[/] No DbContext found. Skipping automatic migration creation.");
                return;
            }
            else if (dbContexts.Count == 1)
            {
                contextName = dbContexts[0].className;
            }
            else
            {
                // Ask the user which DbContext to use
                var choices = dbContexts.Select(d => $"{d.className} ({d.relativePath})").ToList();
                var selected = AnsiConsole.Prompt(
                    new Spectre.Console.SelectionPrompt<string>()
                        .Title("Multiple DbContexts found. Select one for the migration:")
                        .AddChoices(choices)
                );
                var idx = choices.IndexOf(selected);
                if (idx >= 0) contextName = dbContexts[idx].className;
            }

            var args = contextName != null
                ? $"ef migrations add Add{entityName} --context {contextName}"
                : $"ef migrations add Add{entityName}";

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = args,
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            AnsiConsole.MarkupLine($"[cyan]Creating migration:[/] Add{entityName} {(contextName != null ? $"(Context: {contextName})" : string.Empty)}");
            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc != null)
            {
                await proc.WaitForExitAsync();
                if (proc.ExitCode == 0)
                {
                    AnsiConsole.MarkupLine("[green]✓[/] Migration created");
                }
                else
                {
                    AnsiConsole.MarkupLine("[yellow]⚠[/] Failed to create migration automatically. You can run it manually:");
                    AnsiConsole.MarkupLine($"    dotnet ef migrations add Add{entityName}{(contextName != null ? $" --context {contextName}" : string.Empty)}");
                }
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]⚠[/] Skipped automatic migration creation: {ex.Message}");
        }
    }

    private static List<(string className, string relativePath)> FindDbContextCandidates(string workingDir)
    {
        var results = new List<(string, string)>();
        var dataDir = Path.Combine(workingDir, "Data");
        if (!Directory.Exists(dataDir)) return results;

        foreach (var file in Directory.GetFiles(dataDir, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            // Very simple heuristics: class X : DbContext OR : IdentityDbContext
            var lines = text.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains(": DbContext") || line.Contains("IdentityDbContext"))
                {
                    // Try extract class name
                    var idx = line.IndexOf("class ");
                    if (idx >= 0)
                    {
                        var rest = line.Substring(idx + 6).Trim();
                        var name = rest.Split(new[]{' ', ':'}, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            var rel = Path.GetRelativePath(workingDir, file);
                            results.Add((name!, rel));
                        }
                    }
                }
            }
        }

        // Distinct by className
        return results
            .GroupBy(r => r.Item1)
            .Select(g => g.First())
            .ToList();
    }
    private static async Task GenerateControllerAsync(string entityName, string projectName, List<FieldDefinition> fields, bool force)
    {
        var templatePath = Path.Combine(AppContext.BaseDirectory, "templates", "generate", "controller");
        
        if (!Directory.Exists(templatePath))
        {
            throw new DirectoryNotFoundException($"Template directory not found: {templatePath}");
        }
        
        // Check for existing files if not forcing
        if (!force)
        {
            var controllerFile = Path.Combine("Controllers", $"{entityName}Controller.cs");
            if (File.Exists(controllerFile))
            {
                var overwrite = AnsiConsole.Confirm($"[yellow]File already exists:[/] {controllerFile}\nOverwrite all controller files?", false);
                if (!overwrite)
                {
                    throw new OperationCanceledException("Controller generation cancelled by user");
                }
            }
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
        
        // Generate bulk operations content (server-driven with session)
        var bulkSelectHeader = FieldHelper.GenerateBulkSelectHeader(entityName);
        var bulkSelectCell = FieldHelper.GenerateBulkSelectCell(entityName, entityNameLower);
        var bulkSelectionScript = FieldHelper.GenerateBulkSelectionScript(entityName, entityNameLower);
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
                
                await GenerateFileFromTemplateAsync(
                    Path.Combine(templatePath, "Views", "_EntityCheckboxCell.cshtml.template"),
                    Path.Combine("Views", entityName, $"_{entityName}CheckboxCell.cshtml"),
                    variables,
                    ctx
                );
                
                await GenerateFileFromTemplateAsync(
                    Path.Combine(templatePath, "Views", "_BulkActionsBar.cshtml.template"),
                    Path.Combine("Views", entityName, "_BulkActionsBar.cshtml"),
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
