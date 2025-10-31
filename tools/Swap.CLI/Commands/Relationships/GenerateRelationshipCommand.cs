using System.CommandLine;
using System.CommandLine.Invocation;
using Spectre.Console;
using Swap.CLI.Commands.Relationships.Models;

namespace Swap.CLI.Commands.Relationships;

/// <summary>
/// Command to generate relationships between entities
/// Usage: swap generate relationship --source Order --target Customer --type one-to-many
/// </summary>
public static class GenerateRelationshipCommand
{
    public static Command Create()
    {
        var command = new Command("relationship", "Generate a relationship between two entities");
        command.AddAlias("rel");

        // Required options
        var sourceOption = new Option<string?>(
            aliases: new[] { "--source", "-s" },
            description: "The source entity (e.g., Order)");

        var targetOption = new Option<string?>(
            aliases: new[] { "--target", "-t" },
            description: "The target entity (e.g., Customer)");

        var typeOption = new Option<string?>(
            "--type",
            description: "Relationship type: one-to-many, many-to-one, many-to-many, one-to-one");

        // Configuration options
        var requiredOption = new Option<bool>(
            "--required",
            description: "Make the foreign key required (NOT NULL)");

        var onDeleteOption = new Option<string>(
            "--on-delete",
            () => "restrict",
            description: "Delete behavior: cascade, restrict, set-null");

        var displayOption = new Option<string?>(
            "--display",
            description: "Field to display in dropdowns (e.g., Name, Title)");

        var fkOption = new Option<string?>(
            "--fk",
            description: "Foreign key property name (e.g., CustomerId)");

        var navOption = new Option<string?>(
            "--nav",
            description: "Navigation property name");

        var inverseOption = new Option<string?>(
            "--inverse",
            description: "Inverse navigation property name");

        // Many-to-many options
        var junctionOption = new Option<string?>(
            "--junction",
            description: "Junction table name (for many-to-many)");

        var junctionPropsOption = new Option<string?>(
            "--junction-props",
            description: "Additional junction table properties (comma-separated: CreatedAt:datetime,CreatedBy:string)");

        // One-to-one options
        var principalOption = new Option<string?>(
            "--principal",
            description: "Principal entity (for one-to-one)");

        var dependentOption = new Option<string?>(
            "--dependent",
            description: "Dependent entity (for one-to-one)");

        // Skip options
        var skipNavOption = new Option<bool>(
            "--skip-nav",
            description: "Skip navigation property generation");

        var skipUIOption = new Option<bool>(
            "--skip-ui",
            description: "Skip UI generation");

        var skipMigrationsOption = new Option<bool>(
            "--skip-migrations",
            description: "Skip automatic migration creation");

        var projectOption = new Option<string?>(
            aliases: new[] { "--project", "-p" },
            description: "Path to project directory (default: current directory)");

        // Add all options
        command.AddOption(sourceOption);
        command.AddOption(targetOption);
        command.AddOption(typeOption);
        command.AddOption(requiredOption);
        command.AddOption(onDeleteOption);
        command.AddOption(displayOption);
        command.AddOption(fkOption);
        command.AddOption(navOption);
        command.AddOption(inverseOption);
        command.AddOption(junctionOption);
        command.AddOption(junctionPropsOption);
        command.AddOption(principalOption);
        command.AddOption(dependentOption);
        command.AddOption(skipNavOption);
        command.AddOption(skipUIOption);
        command.AddOption(skipMigrationsOption);
        command.AddOption(projectOption);

        command.SetHandler(async (InvocationContext context) =>
        {
            var source = context.ParseResult.GetValueForOption(sourceOption);
            var target = context.ParseResult.GetValueForOption(targetOption);
            var type = context.ParseResult.GetValueForOption(typeOption);
            var required = context.ParseResult.GetValueForOption(requiredOption);
            var onDelete = context.ParseResult.GetValueForOption(onDeleteOption) ?? "restrict";
            var display = context.ParseResult.GetValueForOption(displayOption);
            var fk = context.ParseResult.GetValueForOption(fkOption);
            var nav = context.ParseResult.GetValueForOption(navOption);
            var inverse = context.ParseResult.GetValueForOption(inverseOption);
            var junction = context.ParseResult.GetValueForOption(junctionOption);
            var junctionProps = context.ParseResult.GetValueForOption(junctionPropsOption);
            var principal = context.ParseResult.GetValueForOption(principalOption);
            var dependent = context.ParseResult.GetValueForOption(dependentOption);
            var skipNav = context.ParseResult.GetValueForOption(skipNavOption);
            var skipUI = context.ParseResult.GetValueForOption(skipUIOption);
            var skipMigrations = context.ParseResult.GetValueForOption(skipMigrationsOption);
            var project = context.ParseResult.GetValueForOption(projectOption);

            context.ExitCode = await ExecuteAsync(
                source, target, type, required, onDelete, display, fk, nav, inverse,
                junction, junctionProps, principal, dependent,
                skipNav, skipUI, skipMigrations, project);
        });

        return command;
    }

    private static async Task<int> ExecuteAsync(
        string? source, string? target, string? type, bool required, string onDelete,
        string? display, string? fk, string? nav, string? inverse,
        string? junction, string? junctionProps, string? principal, string? dependent,
        bool skipNav, bool skipUI, bool skipMigrations, string? projectPath)
    {
        try
        {
            AnsiConsole.Write(new FigletText("Swap CLI").Color(Color.Blue));
            AnsiConsole.MarkupLine("[bold blue]Generate Relationship[/]");
            AnsiConsole.WriteLine();

            // Build relationship definition
            var definition = BuildDefinition(
                source, target, type, required, onDelete, display, fk, nav, inverse,
                junction, junctionProps, principal, dependent,
                skipNav, skipUI, skipMigrations, projectPath);

            // Validate the relationship definition
            var validator = new RelationshipValidator();
            var validationResult = validator.Validate(definition);

            if (!validationResult.IsValid)
            {
                AnsiConsole.MarkupLine("[red]Validation failed:[/]");
                foreach (var error in validationResult.Errors)
                {
                    AnsiConsole.MarkupLine($"[red]  • {error}[/]");
                }
                return 1;
            }

            AnsiConsole.MarkupLine("[green]✓ Relationship definition is valid[/]");
            AnsiConsole.MarkupLine($"[dim]Source:[/] {definition.SourceEntity}");
            AnsiConsole.MarkupLine($"[dim]Target:[/] {definition.TargetEntity}");
            AnsiConsole.MarkupLine($"[dim]Type:[/] {definition.Type}");
            AnsiConsole.MarkupLine($"[dim]On Delete:[/] {definition.OnDelete}");
            AnsiConsole.MarkupLine($"[dim]Required:[/] {definition.IsRequired}");
            AnsiConsole.WriteLine();

            // Phase 2: One-to-Many and Many-to-One implementation
            if (definition.Type == RelationshipType.OneToMany || definition.Type == RelationshipType.ManyToOne)
            {
                return await GenerateOneToManyAsync(definition);
            }
            else if (definition.Type == RelationshipType.ManyToMany)
            {
                return await GenerateManyToManyAsync(definition);
            }
            else if (definition.Type == RelationshipType.OneToOne)
            {
                return await GenerateOneToOneAsync(definition);
            }

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }

    private static RelationshipDefinition BuildDefinition(
        string? source, string? target, string? type, bool required, string onDelete,
        string? display, string? fk, string? nav, string? inverse,
        string? junction, string? junctionProps, string? principal, string? dependent,
        bool skipNav, bool skipUI, bool skipMigrations, string? projectPath)
    {
        var definition = new RelationshipDefinition
        {
            SourceEntity = source ?? string.Empty,
            TargetEntity = target ?? string.Empty,
            IsRequired = required,
            DisplayField = display,
            ForeignKeyName = fk,
            NavigationProperty = nav,
            InverseNavigation = inverse,
            JunctionTable = junction,
            PrincipalEntity = principal,
            DependentEntity = dependent,
            SkipNavigation = skipNav,
            SkipUI = skipUI,
            SkipMigrations = skipMigrations,
            ProjectPath = projectPath ?? Directory.GetCurrentDirectory()
        };

        // Parse relationship type
        if (!string.IsNullOrWhiteSpace(type))
        {
            definition.Type = type.ToLowerInvariant() switch
            {
                "one-to-many" or "onetomany" or "1:n" => RelationshipType.OneToMany,
                "many-to-one" or "manytoone" or "n:1" => RelationshipType.ManyToOne,
                "many-to-many" or "manytomany" or "n:n" => RelationshipType.ManyToMany,
                "one-to-one" or "onetoone" or "1:1" => RelationshipType.OneToOne,
                _ => throw new InvalidOperationException($"Invalid relationship type: {type}")
            };
        }

        // Parse delete behavior
        definition.OnDelete = onDelete.ToLowerInvariant() switch
        {
            "cascade" => DeleteBehavior.Cascade,
            "restrict" => DeleteBehavior.Restrict,
            "set-null" or "setnull" => DeleteBehavior.SetNull,
            _ => throw new InvalidOperationException($"Invalid delete behavior: {onDelete}")
        };

        // Parse junction properties if provided
        if (!string.IsNullOrWhiteSpace(junctionProps))
        {
            definition.JunctionProperties = new Dictionary<string, string>();
            var props = junctionProps.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var prop in props)
            {
                var parts = prop.Split(':', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    definition.JunctionProperties[parts[0].Trim()] = parts[1].Trim();
                }
            }
        }

        return definition;
    }

    private static async Task<int> GenerateOneToManyAsync(RelationshipDefinition definition)
    {
        try
        {
            return await AnsiConsole.Status().StartAsync("Generating relationship...", async ctx =>
            {
                // Step 1: Verify entities exist
                ctx.Status("Verifying entities exist...");
                
                var sourceExists = EntityModifier.EntityExists(definition.ProjectPath, definition.SourceEntity);
                var targetExists = EntityModifier.EntityExists(definition.ProjectPath, definition.TargetEntity);

                if (!sourceExists)
                {
                    AnsiConsole.MarkupLine($"[red]✗ Source entity not found:[/] Models/{definition.SourceEntity}.cs");
                    return 1;
                }

                if (!targetExists)
                {
                    AnsiConsole.MarkupLine($"[red]✗ Target entity not found:[/] Models/{definition.TargetEntity}.cs");
                    return 1;
                }

                // Step 2: Modify source entity
                ctx.Status($"Modifying {definition.SourceEntity}...");
                var sourcePath = EntityModifier.GetEntityPath(definition.ProjectPath, definition.SourceEntity);
                var updatedSource = await EntityModifier.AddOneToManyPropertiesAsync(
                    sourcePath, definition.SourceEntity, definition);
                await File.WriteAllTextAsync(sourcePath, updatedSource);
                AnsiConsole.MarkupLine($"[green]✓[/] Modified Models/{definition.SourceEntity}.cs");

                // Step 3: Modify target entity
                ctx.Status($"Modifying {definition.TargetEntity}...");
                var targetPath = EntityModifier.GetEntityPath(definition.ProjectPath, definition.TargetEntity);
                var updatedTarget = await EntityModifier.AddOneToManyPropertiesAsync(
                    targetPath, definition.TargetEntity, definition);
                await File.WriteAllTextAsync(targetPath, updatedTarget);
                AnsiConsole.MarkupLine($"[green]✓[/] Modified Models/{definition.TargetEntity}.cs");

                // Step 4: Configure relationship in DbContext
                ctx.Status("Configuring DbContext...");
                var dbContextPath = DbContextModifier.FindDbContextFile(definition.ProjectPath);
                
                if (dbContextPath != null)
                {
                    var updatedDbContext = await DbContextModifier.ConfigureRelationshipAsync(
                        dbContextPath, definition);
                    await File.WriteAllTextAsync(dbContextPath, updatedDbContext);
                    AnsiConsole.MarkupLine($"[green]✓[/] Configured {Path.GetFileName(dbContextPath)}");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[yellow]⚠[/] DbContext not found - skipping configuration");
                }

                // Step 5: Create migration (if not skipped)
                if (!definition.SkipMigrations)
                {
                    ctx.Status("Creating migration...");
                    var migrationName = $"Add{definition.SourceEntity}To{definition.TargetEntity}Relationship";
                    
                    try
                    {
                        var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "dotnet",
                            Arguments = $"ef migrations add {migrationName}",
                            WorkingDirectory = definition.ProjectPath,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        });

                        if (process != null)
                        {
                            await process.WaitForExitAsync();
                            
                            if (process.ExitCode == 0)
                            {
                                AnsiConsole.MarkupLine($"[green]✓[/] Created migration: {migrationName}");
                            }
                            else
                            {
                                var error = await process.StandardError.ReadToEndAsync();
                                AnsiConsole.MarkupLine($"[yellow]⚠[/] Migration creation failed: {error}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[yellow]⚠[/] Could not create migration: {ex.Message}");
                    }
                }

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[green]✓ Relationship generated successfully![/]");
                AnsiConsole.WriteLine();
                
                // Show summary
                AnsiConsole.MarkupLine("[bold]Summary:[/]");
                AnsiConsole.MarkupLine($"  Type: {definition.Type}");
                AnsiConsole.MarkupLine($"  {definition.SourceEntity} → {definition.TargetEntity}");
                
                if (definition.Type == RelationshipType.OneToMany)
                {
                    var fkName = definition.ForeignKeyName ?? $"{definition.TargetEntity}Id";
                    AnsiConsole.MarkupLine($"  FK: {fkName} in {definition.SourceEntity}");
                    if (!definition.SkipNavigation)
                    {
                        var navProp = definition.NavigationProperty ?? definition.TargetEntity;
                        var inverseProp = definition.InverseNavigation ?? EntityModifier.Pluralize(definition.SourceEntity);
                        AnsiConsole.MarkupLine($"  Navigation: {definition.SourceEntity}.{navProp} → {definition.TargetEntity}");
                        AnsiConsole.MarkupLine($"  Inverse: {definition.TargetEntity}.{inverseProp} → ICollection<{definition.SourceEntity}>");
                    }
                }

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[bold]Next steps:[/]");
                AnsiConsole.MarkupLine($"  1. Review the modified entity files");
                if (!definition.SkipMigrations)
                {
                    AnsiConsole.MarkupLine($"  2. Apply migration: [cyan]dotnet ef database update[/]");
                }
                if (!definition.SkipUI)
                {
                    AnsiConsole.MarkupLine($"  3. UI generation not yet implemented (coming soon)");
                }

                return 0;
            });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error generating relationship:[/] {ex.Message}");
            if (ex.StackTrace != null)
            {
                AnsiConsole.MarkupLine($"[dim]{ex.StackTrace}[/]");
            }
            return 1;
        }
    }

    private static async Task<int> GenerateManyToManyAsync(RelationshipDefinition definition)
    {
        try
        {
            return await AnsiConsole.Status().StartAsync("Generating many-to-many relationship...", async ctx =>
            {
                // Step 1: Verify entities exist
                ctx.Status("Verifying entities exist...");
                
                var sourceExists = EntityModifier.EntityExists(definition.ProjectPath, definition.SourceEntity);
                var targetExists = EntityModifier.EntityExists(definition.ProjectPath, definition.TargetEntity);

                if (!sourceExists)
                {
                    AnsiConsole.MarkupLine($"[red]✗ Source entity not found:[/] Models/{definition.SourceEntity}.cs");
                    return 1;
                }

                if (!targetExists)
                {
                    AnsiConsole.MarkupLine($"[red]✗ Target entity not found:[/] Models/{definition.TargetEntity}.cs");
                    return 1;
                }

                // Step 2: Determine junction table name
                ctx.Status("Determining junction table name...");
                var junctionTableName = definition.JunctionTable ?? GenerateJunctionTableName(definition.SourceEntity, definition.TargetEntity);
                AnsiConsole.MarkupLine($"[green]✓[/] Junction table: {junctionTableName}");

                // Step 3: Create junction table entity
                ctx.Status($"Creating junction table entity {junctionTableName}...");
                var junctionEntityPath = Path.Combine(definition.ProjectPath, "Models", $"{junctionTableName}.cs");
                
                if (File.Exists(junctionEntityPath))
                {
                    AnsiConsole.MarkupLine($"[yellow]⚠[/] Junction table entity already exists: {junctionTableName}");
                }
                else
                {
                    var junctionEntityCode = GenerateJunctionEntityCode(
                        definition.SourceEntity, 
                        definition.TargetEntity, 
                        junctionTableName,
                        definition.JunctionProperties);
                    
                    await File.WriteAllTextAsync(junctionEntityPath, junctionEntityCode);
                    AnsiConsole.MarkupLine($"[green]✓[/] Created Models/{junctionTableName}.cs");
                }

                // Step 4: Modify source entity to add collection navigation
                if (!definition.SkipNavigation)
                {
                    ctx.Status($"Modifying {definition.SourceEntity}...");
                    var sourcePath = EntityModifier.GetEntityPath(definition.ProjectPath, definition.SourceEntity);
                    var updatedSource = await EntityModifier.AddManyToManyNavigationAsync(
                        sourcePath, definition.SourceEntity, definition.TargetEntity, definition.NavigationProperty);
                    await File.WriteAllTextAsync(sourcePath, updatedSource);
                    AnsiConsole.MarkupLine($"[green]✓[/] Modified Models/{definition.SourceEntity}.cs");

                    // Step 5: Modify target entity to add collection navigation
                    ctx.Status($"Modifying {definition.TargetEntity}...");
                    var targetPath = EntityModifier.GetEntityPath(definition.ProjectPath, definition.TargetEntity);
                    var updatedTarget = await EntityModifier.AddManyToManyNavigationAsync(
                        targetPath, definition.TargetEntity, definition.SourceEntity, definition.InverseNavigation);
                    await File.WriteAllTextAsync(targetPath, updatedTarget);
                    AnsiConsole.MarkupLine($"[green]✓[/] Modified Models/{definition.TargetEntity}.cs");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[yellow]⚠[/] Skipped navigation properties (--skip-nav)");
                }

                // Step 6: Configure relationship in DbContext
                ctx.Status("Configuring DbContext...");
                var dbContextPath = DbContextModifier.FindDbContextFile(definition.ProjectPath);
                
                if (dbContextPath != null)
                {
                    // Add DbSet for junction table
                    var dbContextWithDbSet = await DbContextModifier.AddDbSetAsync(
                        dbContextPath, junctionTableName);
                    
                    // Configure many-to-many relationship
                    var updatedDbContext = await DbContextModifier.ConfigureManyToManyAsync(
                        dbContextWithDbSet, definition, junctionTableName);
                    
                    await File.WriteAllTextAsync(dbContextPath, updatedDbContext);
                    AnsiConsole.MarkupLine($"[green]✓[/] Configured {Path.GetFileName(dbContextPath)}");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[yellow]⚠[/] DbContext not found - skipping configuration");
                }

                // Step 7: Create migration (if not skipped)
                if (!definition.SkipMigrations)
                {
                    ctx.Status("Creating migration...");
                    var migrationName = $"Add{definition.SourceEntity}{definition.TargetEntity}ManyToMany";
                    
                    try
                    {
                        var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "dotnet",
                            Arguments = $"ef migrations add {migrationName}",
                            WorkingDirectory = definition.ProjectPath,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        });

                        if (process != null)
                        {
                            await process.WaitForExitAsync();
                            
                            if (process.ExitCode == 0)
                            {
                                AnsiConsole.MarkupLine($"[green]✓[/] Created migration: {migrationName}");
                            }
                            else
                            {
                                var error = await process.StandardError.ReadToEndAsync();
                                AnsiConsole.MarkupLine($"[yellow]⚠[/] Migration creation failed: {error}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[yellow]⚠[/] Could not create migration: {ex.Message}");
                    }
                }

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[green]✓ Many-to-many relationship generated successfully![/]");
                AnsiConsole.WriteLine();
                
                // Show summary
                AnsiConsole.MarkupLine("[bold]Summary:[/]");
                AnsiConsole.MarkupLine($"  Type: Many-to-Many");
                AnsiConsole.MarkupLine($"  {definition.SourceEntity} ↔ {definition.TargetEntity}");
                AnsiConsole.MarkupLine($"  Junction Table: {junctionTableName}");
                
                if (!definition.SkipNavigation)
                {
                    var sourceNavProp = definition.NavigationProperty ?? EntityModifier.Pluralize(definition.TargetEntity);
                    var targetNavProp = definition.InverseNavigation ?? EntityModifier.Pluralize(definition.SourceEntity);
                    AnsiConsole.MarkupLine($"  {definition.SourceEntity}.{sourceNavProp} → ICollection<{definition.TargetEntity}>");
                    AnsiConsole.MarkupLine($"  {definition.TargetEntity}.{targetNavProp} → ICollection<{definition.SourceEntity}>");
                }

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[bold]Next steps:[/]");
                AnsiConsole.MarkupLine($"  1. Review the generated junction table: Models/{junctionTableName}.cs");
                AnsiConsole.MarkupLine($"  2. Review the modified entity files");
                if (!definition.SkipMigrations)
                {
                    AnsiConsole.MarkupLine($"  3. Apply migration: [cyan]dotnet ef database update[/]");
                }
                if (!definition.SkipUI)
                {
                    AnsiConsole.MarkupLine($"  4. Regenerate controllers: [cyan]swap g c {definition.SourceEntity} --force[/]");
                }

                return 0;
            });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error generating many-to-many relationship:[/] {ex.Message}");
            if (ex.StackTrace != null)
            {
                AnsiConsole.MarkupLine($"[dim]{ex.StackTrace}[/]");
            }
            return 1;
        }
    }

    private static string GenerateJunctionTableName(string sourceEntity, string targetEntity)
    {
        // Sort alphabetically to ensure consistent naming
        var entities = new[] { sourceEntity, targetEntity }.OrderBy(e => e).ToArray();
        return $"{entities[0]}{entities[1]}";
    }

    private static string GenerateJunctionEntityCode(
        string sourceEntity, 
        string targetEntity, 
        string junctionTableName,
        Dictionary<string, string>? additionalProperties)
    {
        var sourceFk = $"{sourceEntity}Id";
        var targetFk = $"{targetEntity}Id";
        
        var additionalPropsCode = string.Empty;
        if (additionalProperties != null && additionalProperties.Count > 0)
        {
            foreach (var prop in additionalProperties)
            {
                var propType = MapPropertyType(prop.Value);
                additionalPropsCode += $"\n    public {propType} {prop.Key} {{ get; set; }}";
            }
        }

        return $@"namespace Models;

public class {junctionTableName}
{{
    public int {sourceFk} {{ get; set; }}
    public {sourceEntity}? {sourceEntity} {{ get; set; }}

    public int {targetFk} {{ get; set; }}
    public {targetEntity}? {targetEntity} {{ get; set; }}{additionalPropsCode}
}}
";
    }

    private static string MapPropertyType(string typeString)
    {
        return typeString.ToLowerInvariant() switch
        {
            "string" => "string",
            "int" => "int",
            "long" => "long",
            "bool" => "bool",
            "boolean" => "bool",
            "datetime" => "DateTime",
            "date" => "DateTime",
            "decimal" => "decimal",
            "double" => "double",
            "float" => "float",
            "guid" => "Guid",
            _ => "string"
        };
    }

    private static async Task<int> GenerateOneToOneAsync(RelationshipDefinition definition)
    {
        try
        {
            return await AnsiConsole.Status().StartAsync("Generating one-to-one relationship...", async ctx =>
            {
                // Step 1: Verify entities exist
                ctx.Status("Verifying entities exist...");
                
                var sourceExists = EntityModifier.EntityExists(definition.ProjectPath, definition.SourceEntity);
                var targetExists = EntityModifier.EntityExists(definition.ProjectPath, definition.TargetEntity);

                if (!sourceExists)
                {
                    AnsiConsole.MarkupLine($"[red]✗ Source entity not found:[/] Models/{definition.SourceEntity}.cs");
                    return 1;
                }

                if (!targetExists)
                {
                    AnsiConsole.MarkupLine($"[red]✗ Target entity not found:[/] Models/{definition.TargetEntity}.cs");
                    return 1;
                }

                // Step 2: Determine principal and dependent
                string principalEntity;
                string dependentEntity;
                
                if (!string.IsNullOrEmpty(definition.PrincipalEntity))
                {
                    principalEntity = definition.PrincipalEntity;
                    dependentEntity = principalEntity == definition.SourceEntity ? definition.TargetEntity : definition.SourceEntity;
                }
                else if (!string.IsNullOrEmpty(definition.DependentEntity))
                {
                    dependentEntity = definition.DependentEntity;
                    principalEntity = dependentEntity == definition.SourceEntity ? definition.TargetEntity : definition.SourceEntity;
                }
                else
                {
                    // Default: source is dependent (holds FK), target is principal
                    dependentEntity = definition.SourceEntity;
                    principalEntity = definition.TargetEntity;
                }

                ctx.Status($"Principal: {principalEntity}, Dependent: {dependentEntity}");
                AnsiConsole.MarkupLine($"[green]✓[/] Principal: {principalEntity}, Dependent: {dependentEntity}");

                // Step 3: Modify dependent entity (add FK and navigation)
                ctx.Status($"Modifying {dependentEntity}...");
                var dependentPath = EntityModifier.GetEntityPath(definition.ProjectPath, dependentEntity);
                var updatedDependent = await EntityModifier.AddOneToOnePropertiesAsync(
                    dependentPath, dependentEntity, principalEntity, definition, isDependent: true);
                await File.WriteAllTextAsync(dependentPath, updatedDependent);
                AnsiConsole.MarkupLine($"[green]✓[/] Modified Models/{dependentEntity}.cs (FK + navigation)");

                // Step 4: Modify principal entity (add navigation only)
                if (!definition.SkipNavigation)
                {
                    ctx.Status($"Modifying {principalEntity}...");
                    var principalPath = EntityModifier.GetEntityPath(definition.ProjectPath, principalEntity);
                    var updatedPrincipal = await EntityModifier.AddOneToOnePropertiesAsync(
                        principalPath, principalEntity, dependentEntity, definition, isDependent: false);
                    await File.WriteAllTextAsync(principalPath, updatedPrincipal);
                    AnsiConsole.MarkupLine($"[green]✓[/] Modified Models/{principalEntity}.cs (navigation)");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[yellow]⚠[/] Skipped navigation properties (--skip-nav)");
                }

                // Step 5: Configure relationship in DbContext
                ctx.Status("Configuring DbContext...");
                var dbContextPath = DbContextModifier.FindDbContextFile(definition.ProjectPath);
                
                if (dbContextPath != null)
                {
                    var updatedDbContext = await DbContextModifier.ConfigureOneToOneAsync(
                        dbContextPath, definition, principalEntity, dependentEntity);
                    await File.WriteAllTextAsync(dbContextPath, updatedDbContext);
                    AnsiConsole.MarkupLine($"[green]✓[/] Configured {Path.GetFileName(dbContextPath)}");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[yellow]⚠[/] DbContext not found - skipping configuration");
                }

                // Step 6: Create migration (if not skipped)
                if (!definition.SkipMigrations)
                {
                    ctx.Status("Creating migration...");
                    var migrationName = $"Add{definition.SourceEntity}{definition.TargetEntity}OneToOne";
                    
                    try
                    {
                        var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "dotnet",
                            Arguments = $"ef migrations add {migrationName}",
                            WorkingDirectory = definition.ProjectPath,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        });

                        if (process != null)
                        {
                            await process.WaitForExitAsync();
                            
                            if (process.ExitCode == 0)
                            {
                                AnsiConsole.MarkupLine($"[green]✓[/] Created migration: {migrationName}");
                            }
                            else
                            {
                                var error = await process.StandardError.ReadToEndAsync();
                                AnsiConsole.MarkupLine($"[yellow]⚠[/] Migration creation failed: {error}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[yellow]⚠[/] Could not create migration: {ex.Message}");
                    }
                }

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[green]✓ One-to-one relationship generated successfully![/]");
                AnsiConsole.WriteLine();
                
                // Show summary
                var fkName = definition.ForeignKeyName ?? $"{principalEntity}Id";
                var principalNavProp = definition.NavigationProperty ?? dependentEntity;
                var dependentNavProp = definition.InverseNavigation ?? principalEntity;

                AnsiConsole.MarkupLine("[bold]Summary:[/]");
                AnsiConsole.MarkupLine($"  Type: One-to-One");
                AnsiConsole.MarkupLine($"  Principal: {principalEntity}");
                AnsiConsole.MarkupLine($"  Dependent: {dependentEntity}");
                AnsiConsole.MarkupLine($"  FK: {fkName} in {dependentEntity} (unique constraint)");
                
                if (!definition.SkipNavigation)
                {
                    AnsiConsole.MarkupLine($"  {principalEntity}.{principalNavProp} → {dependentEntity}");
                    AnsiConsole.MarkupLine($"  {dependentEntity}.{dependentNavProp} → {principalEntity}");
                }

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[bold]Next steps:[/]");
                AnsiConsole.MarkupLine($"  1. Review the modified entity files");
                if (!definition.SkipMigrations)
                {
                    AnsiConsole.MarkupLine($"  2. Apply migration: [cyan]dotnet ef database update[/]");
                }
                if (!definition.SkipUI)
                {
                    AnsiConsole.MarkupLine($"  3. Regenerate controllers: [cyan]swap g c {dependentEntity} --force[/]");
                }

                return 0;
            });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error generating one-to-one relationship:[/] {ex.Message}");
            if (ex.StackTrace != null)
            {
                AnsiConsole.MarkupLine($"[dim]{ex.StackTrace}[/]");
            }
            return 1;
        }
    }
}
