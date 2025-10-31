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

        var withRelationshipsOption = new Option<bool>(
            aliases: new[] { "--with-relationships" },
            description: "Auto-detect relationships and generate dropdown selects for foreign keys"
        );
        command.AddOption(withRelationshipsOption);
        
        command.SetHandler(async (InvocationContext context) =>
        {
            var name = context.ParseResult.GetValueForArgument(nameArg);
            var fields = context.ParseResult.GetValueForOption(fieldsOption);
            var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            var force = context.ParseResult.GetValueForOption(forceOption);
            var projectPath = context.ParseResult.GetValueForOption(projectOption);
            var addNav = context.ParseResult.GetValueForOption(addNavOption);
            var noMigrations = context.ParseResult.GetValueForOption(noMigrationsOption);
            var withRelationships = context.ParseResult.GetValueForOption(withRelationshipsOption);
            context.ExitCode = await ExecuteAsync(name, fields, dryRun, force, projectPath, addNav, noMigrations, withRelationships);
        });
        
        return command;
    }
    
    private static async Task<int> ExecuteAsync(string entityName, string? fieldsSpec, bool dryRun, bool force, string? projectPath, bool addNav, bool noMigrations, bool withRelationships)
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
            
            await GenerateControllerAsync(entityName, projectName, fields, force, workingDir, withRelationships);

            // Auto-create migration for the new entity (never applies database update)
            bool migrationCreated = false;
            if (!noMigrations)
            {
                migrationCreated = await TryCreateMigrationAsync(workingDir, entityName);
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
            
            // Only show migration command if it wasn't auto-created
            if (!migrationCreated && !noMigrations)
            {
                AnsiConsole.MarkupLine($"  dotnet ef migrations add Add{entityName}");
            }
            
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
    
    private static async Task<bool> TryCreateMigrationAsync(string workingDir, string entityName)
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
                    // Read output asynchronously to prevent deadlock
                    var outputTask = buildProc.StandardOutput.ReadToEndAsync();
                    var errorTask = buildProc.StandardError.ReadToEndAsync();
                    
                    // Add timeout
                    using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(120));
                    try
                    {
                        await buildProc.WaitForExitAsync(cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        buildProc.Kill(true);
                        AnsiConsole.MarkupLine("[red]✗ Build timed out after 120 seconds[/]");
                        return false;
                    }
                    
                    var outp = await outputTask;
                    var err = await errorTask;
                    
                    if (buildProc.ExitCode != 0)
                    {
                        AnsiConsole.MarkupLine("[red]✗ Build failed before migration creation[/]");
                        if (!string.IsNullOrWhiteSpace(outp)) AnsiConsole.WriteLine(outp);
                        if (!string.IsNullOrWhiteSpace(err)) AnsiConsole.WriteLine(err);
                        return false;
                    }
                }
            }

            // Detect available DbContexts
            var dbContexts = FindDbContextCandidates(workingDir);
            string? contextName = null;

            if (dbContexts.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]ℹ[/] No DbContext found. Skipping automatic migration creation.");
                return false;
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
                // CRITICAL: Read output asynchronously to prevent deadlock
                // Buffers can fill up and block WaitForExitAsync if not read
                var outputTask = proc.StandardOutput.ReadToEndAsync();
                var errorTask = proc.StandardError.ReadToEndAsync();
                
                // Add timeout to prevent infinite hangs
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(60));
                try
                {
                    await proc.WaitForExitAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    proc.Kill(true); // Kill entire process tree
                    AnsiConsole.MarkupLine("[red]✗[/] Migration creation timed out after 60 seconds");
                    return false;
                }
                
                var output = await outputTask;
                var error = await errorTask;
                
                if (proc.ExitCode == 0)
                {
                    AnsiConsole.MarkupLine("[green]✓[/] Migration created");
                    return true;
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(output)) AnsiConsole.WriteLine(output);
                    if (!string.IsNullOrWhiteSpace(error)) AnsiConsole.WriteLine(error);
                    AnsiConsole.MarkupLine("[yellow]⚠[/] Failed to create migration automatically. You can run it manually:");
                    AnsiConsole.MarkupLine($"    dotnet ef migrations add Add{entityName}{(contextName != null ? $" --context {contextName}" : string.Empty)}");
                    return false;
                }
            }
            return false;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]⚠[/] Skipped automatic migration creation: {ex.Message}");
            return false;
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
    private static async Task GenerateControllerAsync(string entityName, string projectName, List<FieldDefinition> fields, bool force, string workingDir, bool withRelationships)
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

        // Detect relationships if flag is set
        List<global::Swap.CLI.Commands.Relationships.DetectedRelationship> relationships = new();
        if (withRelationships)
        {
            // First, try to detect from existing model file
            var entityPath = Path.Combine(workingDir, "Models", $"{entityName}.cs");
            if (File.Exists(entityPath))
            {
                relationships = await global::Swap.CLI.Commands.Relationships.RelationshipUIGenerator.DetectRelationshipsAsync(entityPath);
            }
            
            // Also infer relationships from field names (e.g., CustomerId implies Customer relationship)
            foreach (var field in fields)
            {
                if (field.Name.EndsWith("Id") && field.Name.Length > 2)
                {
                    var fkPrefix = field.Name.Substring(0, field.Name.Length - 2); // Remove "Id" (e.g., "Parent" from "ParentId")
                    
                    // Check if FK prefix matches entity name (e.g., CategoryId in Category)
                    bool isSelfReference = string.Equals(fkPrefix, entityName, StringComparison.OrdinalIgnoreCase);
                    string targetEntity = fkPrefix;
                    
                    // Check if target entity file exists
                    var targetPath = Path.Combine(workingDir, "Models", $"{targetEntity}.cs");
                    bool targetExists = File.Exists(targetPath);
                    
                    if (!isSelfReference && !targetExists)
                    {
                        // Target doesn't exist and not self-reference by name
                        // This might be a self-reference with descriptive FK name (e.g., ParentId in Category)
                        var currentEntityPath = Path.Combine(workingDir, "Models", $"{entityName}.cs");
                        if (File.Exists(currentEntityPath))
                        {
                            // Current entity exists, so this is likely self-reference
                            targetEntity = entityName;
                            isSelfReference = true;
                        }
                        else
                        {
                            // Skip - neither target nor current entity exists
                            continue;
                        }
                    }
                    
                    // Use descriptive navigation property name for self-reference
                    var navPropertyName = isSelfReference ? fkPrefix : targetEntity;
                    
                    // Only add if not already detected
                    if (!relationships.Any(r => r.ForeignKeyProperty == field.Name))
                    {
                        var relationship = new global::Swap.CLI.Commands.Relationships.DetectedRelationship
                        {
                            ForeignKeyProperty = field.Name,
                            TargetEntity = targetEntity,
                            NavigationProperty = navPropertyName,
                            IsRequired = field.IsRequired,
                            RelationshipType = global::Swap.CLI.Commands.Relationships.DetectedRelationshipType.ManyToOne,
                            IsSelfReferencing = isSelfReference
                        };
                        
                        relationships.Add(relationship);
                    }
                }
            }
            
            if (relationships.Any())
            {
                AnsiConsole.MarkupLine($"[cyan]ℹ[/] Detected {relationships.Count} relationship(s)");
                foreach (var rel in relationships)
                {
                    if (rel.RelationshipType == global::Swap.CLI.Commands.Relationships.DetectedRelationshipType.ManyToOne)
                    {
                        var selfRefIndicator = rel.IsSelfReferencing ? " (self-reference)" : "";
                        AnsiConsole.MarkupLine($"  • {rel.ForeignKeyProperty} → {rel.TargetEntity}{selfRefIndicator}");
                    }
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
        
        // Pre-detect display fields for all relationships to avoid multiple async calls
        var displayFieldCache = new Dictionary<string, string>();
        if (withRelationships)
        {
            foreach (var rel in relationships.Where(r => r.RelationshipType == global::Swap.CLI.Commands.Relationships.DetectedRelationshipType.ManyToOne))
            {
                if (rel.TargetEntity != null && !displayFieldCache.ContainsKey(rel.TargetEntity))
                {
                    var targetEntityPath = Path.Combine(workingDir, "Models", $"{rel.TargetEntity}.cs");
                    try
                    {
                        AnsiConsole.MarkupLine($"[dim]  Detecting display field for {rel.TargetEntity}...[/]");
                        displayFieldCache[rel.TargetEntity] = await global::Swap.CLI.Commands.Relationships.RelationshipUIGenerator.DetectDisplayFieldAsync(targetEntityPath, rel.TargetEntity);
                        AnsiConsole.MarkupLine($"[dim]  → Using {displayFieldCache[rel.TargetEntity]}[/]");
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[yellow]  Warning: Could not detect display field for {rel.TargetEntity}: {ex.Message}[/]");
                        displayFieldCache[rel.TargetEntity] = "Name"; // Fallback
                    }
                }
            }
        }
        
        // Generate form fields with relationship awareness
        var formFieldsList = new List<string>();
        foreach (var field in fields)
        {
            var relationship = relationships.FirstOrDefault(r => r.ForeignKeyProperty == field.Name);
            if (relationship != null && withRelationships && relationship.TargetEntity != null)
            {
                // FK field - generate dropdown
                var displayField = displayFieldCache.GetValueOrDefault(relationship.TargetEntity, "Id");
                formFieldsList.Add(global::Swap.CLI.Commands.Relationships.RelationshipUIGenerator.GenerateDropdownFormField(relationship, displayField));
            }
            else
            {
                // Regular field
                formFieldsList.Add(FieldHelper.GenerateFormField(field));
            }
        }
        var formFields = string.Join("\n\n", formFieldsList);
        
        // Generate table headers and cells with relationship awareness
        var tableHeadersList = new List<string>();
        var tableCellsList = new List<string>();
        var detailsFieldsList = new List<string>();
        
        foreach (var field in fields)
        {
            var relationship = relationships.FirstOrDefault(r => r.ForeignKeyProperty == field.Name);
            if (relationship != null && withRelationships && relationship.TargetEntity != null)
            {
                // Skip FK column, we'll show the navigation property instead
                var label = global::Swap.CLI.Commands.Relationships.RelationshipUIGenerator.FormatLabel(relationship.TargetEntity);
                var displayField = displayFieldCache.GetValueOrDefault(relationship.TargetEntity, "Id");
                
                // Table header for relationship (using actual variable values, not template placeholders)
                tableHeadersList.Add($@"<th>
                        <button class=""btn btn-ghost btn-xs"" 
                                hx-get=""@Url.Action(""Get{entityName}List"", new {{ sortBy = ""{relationship.NavigationProperty}"", sortOrder = (@Model.SortBy == ""{relationship.NavigationProperty}"" && @Model.SortOrder == ""asc"" ? ""desc"" : ""asc""), searchTerm = @Model.SearchTerm, pageNumber = 1 }})""
                                hx-target=""#{entityNameLower}-list"" 
                                hx-swap=""outerHTML"">
                            {label}
                            @if (Model.SortBy == ""{relationship.NavigationProperty}"")
                            {{
                                <span>@(Model.SortOrder == ""asc"" ? ""↑"" : ""↓"")</span>
                            }}
                        </button>
                    </th>");
                
                // Table cell showing related entity name
                tableCellsList.Add($"<td>@(item.{relationship.NavigationProperty}?.{displayField} ?? \"-\")</td>");
                
                // Details field for relationship
                detailsFieldsList.Add($@"<div class=""mb-2"">
                <span class=""font-semibold"">{label}:</span>
                <span>@(Model.{relationship.NavigationProperty}?.{displayField} ?? ""None"")</span>
            </div>");
            }
            else
            {
                // Regular field
                tableHeadersList.Add(FieldHelper.GenerateTableHeader(field, entityNameLower));
                tableCellsList.Add(FieldHelper.GenerateTableCell(field));
                detailsFieldsList.Add(FieldHelper.GenerateDetailsField(field));
            }
        }
        
        var tableHeaders = string.Join("\n                    ", tableHeadersList);
        var tableCells = string.Join("\n                        ", tableCellsList);
        var detailsFields = string.Join("\n            ", detailsFieldsList);
        var defaultInitialization = FieldHelper.GenerateDefaultInitialization(fields);
        
        // Generate bulk operations content (server-driven with session)
        var bulkSelectHeader = FieldHelper.GenerateBulkSelectHeader(entityName);
        var bulkSelectCell = FieldHelper.GenerateBulkSelectCell(entityName, entityNameLower);
        var bulkSelectionScript = FieldHelper.GenerateBulkSelectionScript(entityName, entityNameLower);
        var bulkActionsBar = FieldHelper.GenerateBulkActionsBar(entityName, entityNameLower);
        var bulkDeleteScript = FieldHelper.GenerateBulkDeleteScript(entityName, entityNameLower);
        
        // Generate ViewBag population for relationships
        var viewBagPopulation = withRelationships && relationships.Any()
            ? global::Swap.CLI.Commands.Relationships.RelationshipUIGenerator.GenerateViewBagPopulation(relationships)
            : "";
        
        // Generate ViewBag population for Edit action (excludes current entity for self-references)
        var viewBagPopulationEdit = withRelationships && relationships.Any()
            ? global::Swap.CLI.Commands.Relationships.RelationshipUIGenerator.GenerateViewBagPopulationForEdit(relationships, entityName)
            : "";
        
        // Generate Include statements for EF Core to load related entities
        var includeStatements = "";
        if (withRelationships && relationships.Any())
        {
            var includes = relationships
                .Where(r => r.RelationshipType == global::Swap.CLI.Commands.Relationships.DetectedRelationshipType.ManyToOne)
                .Select(r => $".Include(e => e.{r.NavigationProperty})");
            includeStatements = string.Join("", includes);
        }
        
        // Setup template variables
        var variables = new Dictionary<string, string>
        {
            { "EntityName", entityName },
            { "EntityNamePlural", global::Swap.CLI.Commands.Relationships.EntityModifier.Pluralize(entityName) },
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
            { "BulkDeleteScript", bulkDeleteScript },
            { "ViewBagPopulation", viewBagPopulation },
            { "ViewBagPopulationEdit", viewBagPopulationEdit },
            { "IncludeStatements", includeStatements }
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
                    var modelContent = GenerateModelFromFields(entityName, projectName, fields, relationships);
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
    
    private static string GenerateModelFromFields(string entityName, string projectName, List<FieldDefinition> fields, List<global::Swap.CLI.Commands.Relationships.DetectedRelationship> relationships)
    {
        var properties = new List<string>();
        
        foreach (var field in fields)
        {
            var nullability = field.IsNullable ? "?" : "";
            var required = field.IsRequired ? "[Required]" : "";
            
            // Add default value for non-nullable strings to avoid CS8618 warning
            var defaultValue = "";
            var needsInitializer = false;
            if (field.Type == "string" && !field.IsNullable)
            {
                defaultValue = " = string.Empty";
                needsInitializer = true;
            }
            
            if (!string.IsNullOrEmpty(required))
            {
                properties.Add($"    {required}");
            }
            
            var line = $"    public {field.Type}{nullability} {field.Name} {{ get; set; }}";
            if (needsInitializer)
            {
                line += defaultValue + ";"; // property initializer requires semicolon
            }
            properties.Add(line);
        }
        
        // Add navigation properties for detected relationships
        foreach (var relationship in relationships.Where(r => r.RelationshipType == global::Swap.CLI.Commands.Relationships.DetectedRelationshipType.ManyToOne))
        {
            var nullability = relationship.IsRequired ? "" : "?";
            properties.Add($"    public {relationship.TargetEntity}{nullability} {relationship.NavigationProperty} {{ get; set; }}");
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
        var pluralEntityName = global::Swap.CLI.Commands.Relationships.EntityModifier.Pluralize(entityName);
        if (content.Contains($"DbSet<{entityName}>") || 
            content.Contains($"DbSet<{projectName}.Models.{entityName}>") ||
            content.Contains($"{pluralEntityName} {{ get; set; }}"))
        {
            AnsiConsole.MarkupLine($"[yellow]Warning:[/] DbSet for {entityName} already exists in DbContext");
            return;
        }
        
        // Find where to insert: after last DbSet but before OnModelCreating
        var onModelCreatingIndex = content.IndexOf("protected override void OnModelCreating", StringComparison.Ordinal);
        var dbSetPattern = "public DbSet<";
        var lastDbSetIndex = content.LastIndexOf(dbSetPattern);
        
        if (lastDbSetIndex == -1)
        {
            throw new InvalidOperationException("Could not find any DbSet properties in DbContext");
        }
        
        // Find the end of the LAST DbSet property
        var lineEnd = -1;
        var searchStart = lastDbSetIndex;
        for (int i = searchStart; i < content.Length; i++)
        {
            // Stop searching if we hit OnModelCreating - we've gone too far
            if (onModelCreatingIndex != -1 && i >= onModelCreatingIndex)
            {
                break;
            }
            
            if ((content[i] == ';' || content[i] == '}') && i + 1 < content.Length)
            {
                // Found property terminator, now find the newline
                var nextNewline = content.IndexOf('\n', i);
                if (nextNewline != -1)
                {
                    lineEnd = nextNewline;
                    break;
                }
            }
        }
        
        if (lineEnd == -1) lineEnd = content.Length;
        
        // Double-check we're not inserting after OnModelCreating
        if (onModelCreatingIndex != -1 && lineEnd > onModelCreatingIndex)
        {
            // Find the line before OnModelCreating instead
            var beforeMethod = content.LastIndexOf('\n', onModelCreatingIndex - 1);
            if (beforeMethod != -1)
            {
                lineEnd = beforeMethod;
            }
        }
        
        // Insert new DbSet property with fully qualified type name to avoid conflicts
        var newDbSet = $"\n    public DbSet<{projectName}.Models.{entityName}> {pluralEntityName} {{ get; set; }}";
        content = content.Insert(lineEnd + 1, newDbSet);
        
        await File.WriteAllTextAsync(dbContextPath, content);
    }
}
