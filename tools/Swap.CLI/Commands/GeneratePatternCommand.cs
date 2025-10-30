using System.CommandLine;
using System.CommandLine.Invocation;
using System.Xml.Linq;
using Spectre.Console;
using Swap.CLI.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Swap.CLI.Commands;

public static class GeneratePatternCommand
{
    public static Command Create()
    {
    var command = new Command("pattern", "Add or remove common patterns on entities (softdelete, auditable, sluggable, timestampable, orderable, publishable, versionable, visibility)");
        command.AddAlias("p");

        var typeArg = new Argument<string>(
            name: "type",
            description: "Pattern type: softdelete, auditable, sluggable, timestampable, orderable, publishable, versionable, or visibility");

        var entityArg = new Argument<string>(
            name: "entity",
            description: "Entity name (e.g., Post)");

        var forceOption = new Option<bool>(
            aliases: new[] { "--force", "-f" },
            description: "Overwrite files without prompting");

        var projectOption = new Option<string?>(
            aliases: new[] { "--project", "-p" },
            description: "Path to project directory (default: current directory)");

        // Deprecated options (hidden). We always use NuGet package mode now.
        var usePackageOption = new Option<bool>(
            aliases: new[] { "--use-package" },
            description: "[DEPRECATED] Always on. Swap now always uses the Swap.Patterns NuGet package."
        ) { IsHidden = true };

        var fallbackOption = new Option<bool>(
            aliases: new[] { "--fallback" },
            description: "[DEPRECATED] No longer supported. Embed mode has been removed."
        ) { IsHidden = true };

        var noMigrationsOption = new Option<bool>(
            aliases: new[] { "--no-migrations" },
            description: "Skip automatic migration creation (you'll need to create migrations manually)"
        );

        command.AddArgument(typeArg);
        command.AddArgument(entityArg);
        command.AddOption(forceOption);
        command.AddOption(projectOption);
        command.AddOption(usePackageOption);
        command.AddOption(fallbackOption);
        command.AddOption(noMigrationsOption);

        command.SetHandler(async (InvocationContext ctx) =>
        {
            var type = ctx.ParseResult.GetValueForArgument(typeArg);
            var entity = ctx.ParseResult.GetValueForArgument(entityArg);
            var force = ctx.ParseResult.GetValueForOption(forceOption);
            var projectPath = ctx.ParseResult.GetValueForOption(projectOption);
            var usePackage = ctx.ParseResult.GetValueForOption(usePackageOption);
            var fallback = ctx.ParseResult.GetValueForOption(fallbackOption);

            // Emit deprecation notices if legacy flags are provided
            if (usePackage)
            {
                AnsiConsole.MarkupLine("[yellow]⚠[/] [bold]--use-package[/] is deprecated and always enabled by default.");
            }
            if (fallback)
            {
                AnsiConsole.MarkupLine("[yellow]⚠[/] [bold]--fallback[/] is deprecated and ignored. Embed mode has been removed.");
            }

            // Enforce NuGet package mode by default; no embedding
            usePackage = true;
            fallback = false;
            var noMigrations = ctx.ParseResult.GetValueForOption(noMigrationsOption);

            ctx.ExitCode = await ExecuteAsync(type, entity, force, projectPath, usePackage, fallback, noMigrations);
        });

        // Subcommands
        command.AddCommand(CreateRemoveSubcommand());

        return command;
    }

    private static async Task<int> ExecuteRemoveAsync(string type, string entity, string? projectPath, bool dropColumns, bool noMigrations)
    {
        var workingDir = !string.IsNullOrEmpty(projectPath)
            ? Path.GetFullPath(projectPath)
            : Directory.GetCurrentDirectory();

        var projectFiles = Directory.GetFiles(workingDir, "*.csproj");
        if (projectFiles.Length == 0)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] No .csproj file found in {workingDir}");
            return 1;
        }

        var modelPath = Path.Combine(workingDir, "Models", $"{entity}.cs");
        if (!File.Exists(modelPath))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Model file not found: {modelPath}");
            return 1;
        }

        type = type.ToLowerInvariant();

        switch (type)
        {
            case "softdelete" or "soft":
                return await RemoveSoftDeleteAsync(workingDir, modelPath, entity, dropColumns, noMigrations);
            case "auditable" or "audit":
                return await RemoveAuditableAsync(workingDir, modelPath, entity, dropColumns, noMigrations);
            case "timestampable" or "timestamp":
                return await RemoveTimestampableAsync(workingDir, modelPath, entity, dropColumns, noMigrations);
            case "sluggable" or "slug":
                return await RemoveSluggableAsync(workingDir, modelPath, entity, dropColumns, noMigrations);
            default:
                AnsiConsole.MarkupLine($"[red]Error:[/] Unknown pattern type: {type}");
                return 1;
        }
    }

    private static async Task<int> RemoveSoftDeleteAsync(string workingDir, string modelPath, string entity, bool dropColumns, bool noMigrations)
    {
        AnsiConsole.MarkupLine($"[cyan]Removing soft delete pattern from {entity}...[/]");
        await ModifyModelForRemovalAsync(modelPath, entity, "ISoftDeletable", new[] { "IsDeleted", "DeletedAt", "DeletedBy" });

        // Update swap-config
        var (cfg, cfgPath) = Swap.CLI.Infrastructure.SwapConfigManager.LoadOrCreate(workingDir);
        Swap.CLI.Infrastructure.SwapConfigManager.RemovePattern(cfg, entity, "SoftDelete");
        Swap.CLI.Infrastructure.SwapConfigManager.Save(cfg, cfgPath);

        // Remove global filter if no entity uses SoftDelete
        if (!Swap.CLI.Infrastructure.SwapConfigManager.IsPatternInUse(cfg, "SoftDelete"))
        {
            await UpdateDbContextRemoveLinesAsync(workingDir, "ConfigureSoftDeleteFilter");
        }

        if (dropColumns && !noMigrations)
        {
            await TryCreateMigrationAsync(workingDir, $"RemoveSoftDeleteFrom{entity}");
        }
        else if (dropColumns)
        {
            AnsiConsole.MarkupLine("[yellow]ℹ[/] Skipping migration creation (--no-migrations). You removed the properties from the model; create a migration to drop columns if desired.");
        }

        AnsiConsole.MarkupLine("[green]✓[/] Soft delete pattern removed");
        return 0;
    }

    private static async Task<int> RemoveAuditableAsync(string workingDir, string modelPath, string entity, bool dropColumns, bool noMigrations)
    {
        AnsiConsole.MarkupLine($"[cyan]Removing auditable pattern from {entity}...[/]");
        await ModifyModelForRemovalAsync(modelPath, entity, "IAuditable", new[] { "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy" });

        var (cfg, cfgPath) = Swap.CLI.Infrastructure.SwapConfigManager.LoadOrCreate(workingDir);
        Swap.CLI.Infrastructure.SwapConfigManager.RemovePattern(cfg, entity, "Auditable");
        Swap.CLI.Infrastructure.SwapConfigManager.Save(cfg, cfgPath);

        // Remove interceptor and accessor registration if unused
        if (!Swap.CLI.Infrastructure.SwapConfigManager.IsPatternInUse(cfg, "Auditable"))
        {
            await UpdateDbContextRemoveLinesAsync(workingDir, "CreateAuditInterceptor");
            await UpdateProgramRemoveHttpContextAccessorAsync(workingDir);
        }

        if (dropColumns && !noMigrations)
        {
            await TryCreateMigrationAsync(workingDir, $"RemoveAuditableFrom{entity}");
        }
        else if (dropColumns)
        {
            AnsiConsole.MarkupLine("[yellow]ℹ[/] Skipping migration creation (--no-migrations). You removed the properties from the model; create a migration to drop columns if desired.");
        }

        AnsiConsole.MarkupLine("[green]✓[/] Auditable pattern removed");
        return 0;
    }

    private static async Task<int> RemoveTimestampableAsync(string workingDir, string modelPath, string entity, bool dropColumns, bool noMigrations)
    {
        AnsiConsole.MarkupLine($"[cyan]Removing timestampable pattern from {entity}...[/]");
        await ModifyModelForRemovalAsync(modelPath, entity, "ITimestampable", new[] { "CreatedAt", "UpdatedAt" });

        var (cfg, cfgPath) = Swap.CLI.Infrastructure.SwapConfigManager.LoadOrCreate(workingDir);
        Swap.CLI.Infrastructure.SwapConfigManager.RemovePattern(cfg, entity, "Timestampable");
        Swap.CLI.Infrastructure.SwapConfigManager.Save(cfg, cfgPath);

        if (!Swap.CLI.Infrastructure.SwapConfigManager.IsPatternInUse(cfg, "Timestampable"))
        {
            await UpdateDbContextRemoveLinesAsync(workingDir, "TimestampInterceptor");
        }

        if (dropColumns && !noMigrations)
        {
            await TryCreateMigrationAsync(workingDir, $"RemoveTimestampableFrom{entity}");
        }
        else if (dropColumns)
        {
            AnsiConsole.MarkupLine("[yellow]ℹ[/] Skipping migration creation (--no-migrations). You removed the properties from the model; create a migration to drop columns if desired.");
        }

        AnsiConsole.MarkupLine("[green]✓[/] Timestampable pattern removed");
        return 0;
    }

    private static async Task<int> RemoveSluggableAsync(string workingDir, string modelPath, string entity, bool dropColumns, bool noMigrations)
    {
        AnsiConsole.MarkupLine($"[cyan]Removing sluggable pattern from {entity}...[/]");
        await ModifyModelForRemovalAsync(modelPath, entity, "ISluggable", new[] { "Slug" });

        var (cfg, cfgPath) = Swap.CLI.Infrastructure.SwapConfigManager.LoadOrCreate(workingDir);
        Swap.CLI.Infrastructure.SwapConfigManager.RemovePattern(cfg, entity, "Sluggable");
        Swap.CLI.Infrastructure.SwapConfigManager.Save(cfg, cfgPath);

        // Remove slug index line for this entity
        await UpdateDbContextRemoveSlugIndexForEntityAsync(workingDir, entity);

        if (dropColumns && !noMigrations)
        {
            await TryCreateMigrationAsync(workingDir, $"Remove{entity}Slug");
        }
        else if (dropColumns)
        {
            AnsiConsole.MarkupLine("[yellow]ℹ[/] Skipping migration creation (--no-migrations). You removed the property from the model; create a migration to drop the column/index if desired.");
        }

        AnsiConsole.MarkupLine("[green]✓[/] Sluggable pattern removed");
        return 0;
    }

    private static async Task ModifyModelForRemovalAsync(string modelPath, string entity, string interfaceName, string[] propertyNames)
    {
        var code = await File.ReadAllTextAsync(modelPath);
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetCompilationUnitRoot();

        var originalClass = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault(c => c.Identifier.Text == entity);
        if (originalClass == null) return;
        var classDecl = originalClass;

        // Remove interface from base list
        if (classDecl.BaseList != null)
        {
            var newTypes = new SeparatedSyntaxList<BaseTypeSyntax>();
            foreach (var t in classDecl.BaseList.Types)
            {
                if (!t.ToString().Contains(interfaceName))
                {
                    newTypes = newTypes.Add(t);
                }
            }
            if (newTypes.Count == 0)
            {
                classDecl = classDecl.WithBaseList(null);
            }
            else
            {
                classDecl = classDecl.WithBaseList(classDecl.BaseList.WithTypes(newTypes));
            }
        }

        // Remove properties
        var toRemove = classDecl.Members
            .OfType<PropertyDeclarationSyntax>()
            .Where(p => propertyNames.Contains(p.Identifier.Text))
            .ToList();
        if (toRemove.Count > 0)
        {
            classDecl = classDecl.RemoveNodes(toRemove, SyntaxRemoveOptions.KeepNoTrivia);
        }

        // Write back
    root = root.ReplaceNode(originalClass, classDecl!);
        var updated = root.NormalizeWhitespace().ToFullString();
        // Fallback cleanup: if a stray colon remains after class name, remove it
        var strayColonPattern = $@"public\s+class\s+{System.Text.RegularExpressions.Regex.Escape(entity)}\s*:\\s*(\r?\n)?";
        updated = System.Text.RegularExpressions.Regex.Replace(updated, strayColonPattern, $"public class {entity}$1");
        updated = updated.Replace($"public class {entity} :", $"public class {entity}");
        await File.WriteAllTextAsync(modelPath, updated);
    }

    private static async Task UpdateDbContextRemoveLinesAsync(string workingDir, string containsText)
    {
        var dataDir = Path.Combine(workingDir, "Data");
        if (!Directory.Exists(dataDir)) return;

        var candidates = Directory.GetFiles(dataDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => File.ReadAllText(f).Contains(": DbContext") || File.ReadAllText(f).Contains("IdentityDbContext"))
            .ToList();

        foreach (var path in candidates)
        {
            var text = await File.ReadAllTextAsync(path);
            var newText = System.Text.RegularExpressions.Regex.Replace(text, @$"^[\ \t]*.*{containsText}.*\r?\n", string.Empty, System.Text.RegularExpressions.RegexOptions.Multiline);
            if (!ReferenceEquals(text, newText))
            {
                await File.WriteAllTextAsync(path, newText);
                AnsiConsole.MarkupLine($"[green]✓[/] Removed DbContext wiring: {containsText}");
            }
        }
    }

    private static async Task UpdateDbContextRemoveSlugIndexForEntityAsync(string workingDir, string entity)
    {
        var dataDir = Path.Combine(workingDir, "Data");
        if (!Directory.Exists(dataDir)) return;

        var candidates = Directory.GetFiles(dataDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => File.ReadAllText(f).Contains(": DbContext") || File.ReadAllText(f).Contains("IdentityDbContext"))
            .ToList();

        foreach (var path in candidates)
        {
            var text = await File.ReadAllTextAsync(path);
            var pattern = $@"^.*modelBuilder\.Entity<{entity}>\(\)\.HasIndex\(e => e\.Slug\)\.IsUnique\(\);\s*\r?\n";
            var newText = System.Text.RegularExpressions.Regex.Replace(text, pattern, string.Empty, System.Text.RegularExpressions.RegexOptions.Multiline);
            if (newText != text)
            {
                await File.WriteAllTextAsync(path, newText);
                AnsiConsole.MarkupLine("[green]✓[/] Removed Slug unique index from DbContext");
            }
        }
    }

    private static async Task UpdateProgramRemoveHttpContextAccessorAsync(string workingDir)
    {
        var programPath = Path.Combine(workingDir, "Program.cs");
        if (!File.Exists(programPath)) return;
        var text = await File.ReadAllTextAsync(programPath);
        var newText = System.Text.RegularExpressions.Regex.Replace(text, @"^.*AddHttpContextAccessor\(\).*\r?\n", string.Empty, System.Text.RegularExpressions.RegexOptions.Multiline);
        if (newText != text)
        {
            await File.WriteAllTextAsync(programPath, newText);
            AnsiConsole.MarkupLine("[green]✓[/] Removed IHttpContextAccessor registration from Program.cs");
        }
    }

    private static Command CreateRemoveSubcommand()
    {
        var remove = new Command("remove", "Remove a pattern from an entity");
        remove.AddAlias("rm");

        var typeArg = new Argument<string>(
            name: "type",
            description: "Pattern type to remove: softdelete, auditable, sluggable, timestampable, orderable, publishable, versionable, visibility");

        var entityArg = new Argument<string>(
            name: "entity",
            description: "Entity name (e.g., Post)");

        var projectOption = new Option<string?>(
            aliases: new[] { "--project", "-p" },
            description: "Path to project directory (default: current directory)");

        var dropColumnsOption = new Option<bool>(
            aliases: new[] { "--drop-columns" },
            description: "Also drop columns/indexes by creating a migration");

        var noMigrationsOption = new Option<bool>(
            aliases: new[] { "--no-migrations" },
            description: "Skip automatic migration creation when dropping columns");

        remove.AddArgument(typeArg);
        remove.AddArgument(entityArg);
        remove.AddOption(projectOption);
        remove.AddOption(dropColumnsOption);
        remove.AddOption(noMigrationsOption);

        remove.SetHandler(async (InvocationContext ctx) =>
        {
            var type = ctx.ParseResult.GetValueForArgument(typeArg);
            var entity = ctx.ParseResult.GetValueForArgument(entityArg);
            var projectPath = ctx.ParseResult.GetValueForOption(projectOption);
            var dropColumns = ctx.ParseResult.GetValueForOption(dropColumnsOption);
            var noMigrations = ctx.ParseResult.GetValueForOption(noMigrationsOption);

            ctx.ExitCode = await ExecuteRemoveAsync(type, entity, projectPath, dropColumns, noMigrations);
        });

        return remove;
    }

    public static async Task<int> ExecuteAsync(string type, string entity, bool force, string? projectPath, bool usePackage, bool fallback, bool noMigrations)
    {
        var workingDir = !string.IsNullOrEmpty(projectPath)
            ? Path.GetFullPath(projectPath)
            : Directory.GetCurrentDirectory();

        var projectFiles = Directory.GetFiles(workingDir, "*.csproj");
        if (projectFiles.Length == 0)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] No .csproj file found in {workingDir}");
            return 1;
        }

        var projectFile = projectFiles[0];

        // Check for pattern compatibility before applying
        var modelPath = Path.Combine(workingDir, "Models", $"{entity}.cs");
        if (File.Exists(modelPath))
        {
            var compatibilityIssue = await CheckPatternCompatibilityAsync(modelPath, entity, type);
            if (compatibilityIssue != null)
            {
                AnsiConsole.MarkupLine($"[red]✗ Pattern conflict:[/] {compatibilityIssue}");
                return 1;
            }
        }

        try
        {
            return type.ToLower() switch
            {
                "softdelete" or "soft" => await ApplySoftDeleteAsync(workingDir, projectFile, entity, force, usePackage, fallback, noMigrations),
                "auditable" or "audit" => await ApplyAuditableAsync(workingDir, projectFile, entity, force, usePackage, fallback, noMigrations),
                "sluggable" or "slug" => await ApplySluggableAsync(workingDir, projectFile, entity, force, usePackage, fallback, noMigrations),
                "timestampable" or "timestamp" => await ApplyTimestampableAsync(workingDir, projectFile, entity, force, usePackage, fallback, noMigrations),
                "orderable" or "order" => await ApplyOrderableAsync(workingDir, projectFile, entity, force, usePackage, fallback, noMigrations),
                "publishable" or "publish" or "pub" => await ApplyPublishableAsync(workingDir, projectFile, entity, force, usePackage, fallback, noMigrations),
                "versionable" or "version" or "ver" => await ApplyVersionableAsync(workingDir, projectFile, entity, force, usePackage, fallback, noMigrations),
                "visibility" or "visible" or "vis" => await ApplyVisibilityAsync(workingDir, projectFile, entity, force, usePackage, fallback, noMigrations),
                _ => throw new Exception($"Unknown pattern type: {type}. Use softdelete, auditable, sluggable, timestampable, orderable, publishable, versionable, or visibility")
            };
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> ApplySoftDeleteAsync(string workingDir, string projectFile, string entity, bool force, bool usePackage, bool fallback, bool noMigrations)
    {
        AnsiConsole.MarkupLine($"[cyan]Adding soft delete pattern to {entity}...[/]");

        // Find entity model
        var modelPath = Path.Combine(workingDir, "Models", $"{entity}.cs");
        if (!File.Exists(modelPath))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Model file not found: {modelPath}");
            return 1;
        }

        // Package path (optional)
        if (usePackage)
        {
            var packageInstalled = await EnsureSwapPatternsPackageAsync(projectFile, fallback);
            if (!packageInstalled && !fallback)
            {
                return 1; // Strict mode: fail if package installation fails
            }
            // If fallback is true and package failed, packageInstalled will be false
            // and we'll use embedded code below
            usePackage = packageInstalled; // Switch to embed mode if package failed
        }

        // Modify the entity model
        var modelContent = await File.ReadAllTextAsync(modelPath);
        var tree = CSharpSyntaxTree.ParseText(modelContent);
        var root = await tree.GetRootAsync();

        // Find the class
        var classDecl = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(c => c.Identifier.Text == entity);

        if (classDecl == null)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Class {entity} not found in {modelPath}");
            return 1;
        }

        if (classDecl.BaseList?.Types.Any(t => t.ToString().Contains("ISoftDeletable")) == true)
        {
            AnsiConsole.MarkupLine($"[yellow]Warning:[/] {entity} already implements ISoftDeletable");
            return 0;
        }

        // Package mode: add interface + using; Embed mode: properties only
        if (usePackage)
        {
            var hasUsing = root.DescendantNodes()
                .OfType<UsingDirectiveSyntax>()
                .Any(u => u.Name?.ToString() == "Swap.Patterns.SoftDelete");

            var compilationUnit = (CompilationUnitSyntax)root;
            if (!hasUsing)
            {
                var usingDirective = SyntaxFactory.UsingDirective(
                    SyntaxFactory.ParseName("Swap.Patterns.SoftDelete"))
                    .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
                compilationUnit = compilationUnit.AddUsings(usingDirective);
                root = compilationUnit;
            }

            var baseList = classDecl.BaseList ?? SyntaxFactory.BaseList();
            baseList = baseList.AddTypes(
                SyntaxFactory.SimpleBaseType(
                    SyntaxFactory.ParseTypeName(" ISoftDeletable")));
            classDecl = classDecl.WithBaseList(baseList);
        }

        // Add properties if missing
        var existingProps = classDecl.Members.OfType<PropertyDeclarationSyntax>().Select(p => p.Identifier.Text).ToHashSet();
        var toAdd = new List<MemberDeclarationSyntax>();
        var softProps = new (string Name, string Type)[]
        {
            ("IsDeleted", "bool"),
            ("DeletedAt", "DateTime?"),
            ("DeletedBy", "string?")
        };

        for (int i = 0; i < softProps.Length; i++)
        {
            var (name, type) = softProps[i];
            if (!existingProps.Contains(name))
            {
                var prop = SyntaxFactory.PropertyDeclaration(
                    SyntaxFactory.ParseTypeName(type),
                    name)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .AddAccessorListAccessors(
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                    );

                if (i == 0)
                {
                    prop = prop.WithLeadingTrivia(
                        SyntaxFactory.CarriageReturnLineFeed,
                        SyntaxFactory.Comment("    // Soft delete properties"),
                        SyntaxFactory.CarriageReturnLineFeed
                    );
                }

                toAdd.Add(prop);
            }
        }

        if (toAdd.Count > 0)
        {
            classDecl = classDecl.AddMembers(toAdd.ToArray());
        }

        // Replace and write back
        root = root.ReplaceNode(root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == entity), classDecl);
        var formatted = root.NormalizeWhitespace().ToFullString();
        await File.WriteAllTextAsync(modelPath, formatted);

        // Optional format
        try
        {
            var formatProcess = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"format \"{projectFile}\" --include \"{modelPath}\"",
                    WorkingDirectory = workingDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            formatProcess.Start();
            await formatProcess.WaitForExitAsync();
        }
        catch { }

        // Ensure DbContext has global soft-delete filter
        await UpdateDbContextForSoftDeleteAsync(workingDir);

        // Auto-create migration (no DB update)
        if (!noMigrations)
        {
            await TryCreateMigrationAsync(workingDir, $"AddSoftDeleteTo{entity}");
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]ℹ[/] Migration creation skipped (--no-migrations flag)");
            AnsiConsole.MarkupLine("[dim]Create migration manually:[/] [cyan]dotnet ef migrations add AddSoftDeleteTo{0}[/]", entity);
        }

        // Update swap-config.json
        try
        {
            var (cfg, cfgPath) = Swap.CLI.Infrastructure.SwapConfigManager.LoadOrCreate(workingDir);
            Swap.CLI.Infrastructure.SwapConfigManager.RecordPattern(cfg, entity, "SoftDelete", new Dictionary<string, bool>{{"SoftDeleteFilter", true}});
            Swap.CLI.Infrastructure.SwapConfigManager.Save(cfg, cfgPath);
        }
        catch { }

        AnsiConsole.MarkupLine($"[green]✓[/] Soft delete pattern applied successfully!");
        if (!noMigrations)
        {
            AnsiConsole.MarkupLine($"[cyan]Next steps:[/]");
            AnsiConsole.MarkupLine($"  1. (Optional) Update database: [grey]dotnet ef database update[/]");
        }

        return 0;
    }

    private static async Task<int> ApplyAuditableAsync(string workingDir, string projectFile, string entity, bool force, bool usePackage, bool fallback, bool noMigrations)
    {
        AnsiConsole.MarkupLine($"[cyan]Adding auditable pattern to {entity}...[/]");

        // Find entity model
        var modelPath = Path.Combine(workingDir, "Models", $"{entity}.cs");
        if (!File.Exists(modelPath))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Model file not found: {modelPath}");
            return 1;
        }

        // Package mode optional
        if (usePackage)
        {
            var packageInstalled = await EnsureSwapPatternsPackageAsync(projectFile, fallback);
            if (!packageInstalled && !fallback)
            {
                return 1; // Strict mode: fail if package installation fails
            }
            usePackage = packageInstalled; // Switch to embed mode if package failed
        }

        // Modify the entity model
        var modelContent = await File.ReadAllTextAsync(modelPath);
        var tree = CSharpSyntaxTree.ParseText(modelContent);
        var root = await tree.GetRootAsync();

        // Check if already implements IAuditable
        var classDecl = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(c => c.Identifier.Text == entity);

        if (classDecl == null)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Class {entity} not found in {modelPath}");
            return 1;
        }

        if (classDecl.BaseList?.Types.Any(t => t.ToString().Contains("IAuditable")) == true)
        {
            AnsiConsole.MarkupLine($"[yellow]Warning:[/] {entity} already implements IAuditable");
            return 0;
        }

        // Package mode: add interface + using; Embed mode: properties only
        if (usePackage)
        {
            var hasUsing = root.DescendantNodes()
                .OfType<UsingDirectiveSyntax>()
                .Any(u => u.Name?.ToString() == "Swap.Patterns.Auditable");

            CompilationUnitSyntax compilationUnit = (CompilationUnitSyntax)root;
            if (!hasUsing)
            {
                var usingDirective = SyntaxFactory.UsingDirective(
                    SyntaxFactory.ParseName("Swap.Patterns.Auditable"))
                    .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
                compilationUnit = compilationUnit.AddUsings(usingDirective);
                root = compilationUnit;
            }

            var newBaseList = classDecl.BaseList ?? SyntaxFactory.BaseList();
            newBaseList = newBaseList.AddTypes(
                SyntaxFactory.SimpleBaseType(
                    SyntaxFactory.ParseTypeName(" IAuditable")));
            classDecl = classDecl.WithBaseList(newBaseList);
        }

        // Add properties if they don't exist
        var hasCreatedAt = classDecl.Members
            .OfType<PropertyDeclarationSyntax>()
            .Any(p => p.Identifier.Text == "CreatedAt");

        if (!hasCreatedAt)
        {
            var properties = new[]
            {
                ("CreatedAt", "DateTime"),
                ("CreatedBy", "string?"),
                ("UpdatedAt", "DateTime?"),
                ("UpdatedBy", "string?")
            };

            var newMembers = new List<MemberDeclarationSyntax>();
            for (int i = 0; i < properties.Length; i++)
            {
                var (name, type) = properties[i];
                var hasProperty = classDecl.Members
                    .OfType<PropertyDeclarationSyntax>()
                    .Any(p => p.Identifier.Text == name);

                if (!hasProperty)
                {
                    var prop = SyntaxFactory.PropertyDeclaration(
                        SyntaxFactory.ParseTypeName(type),
                        name)
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                        .AddAccessorListAccessors(
                            SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                            SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                        );

                    if (i == 0)
                    {
                        prop = prop.WithLeadingTrivia(
                            SyntaxFactory.CarriageReturnLineFeed,
                            SyntaxFactory.Comment("    // IAuditable properties"),
                            SyntaxFactory.CarriageReturnLineFeed
                        );
                    }

                    newMembers.Add(prop);
                }
            }

            classDecl = classDecl.AddMembers(newMembers.ToArray());
        }

        // Replace class in tree and write back
        root = root.ReplaceNode(root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == entity), classDecl);
        var outCode = root.NormalizeWhitespace().ToFullString();
        await File.WriteAllTextAsync(modelPath, outCode);

        // Optional format
        try
        {
            var formatProcess = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"format \"{projectFile}\" --include \"{modelPath}\"",
                    WorkingDirectory = workingDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            formatProcess.Start();
            await formatProcess.WaitForExitAsync();
        }
        catch { }

    // Auto-create migration (no DB update)
    if (!noMigrations)
    {
        await TryCreateMigrationAsync(workingDir, $"AddAuditableTo{entity}");
    }
    else
    {
        AnsiConsole.MarkupLine("[yellow]ℹ[/] Migration creation skipped (--no-migrations flag)");
        AnsiConsole.MarkupLine("[dim]Create migration manually:[/] [cyan]dotnet ef migrations add AddAuditableTo{0}[/]", entity);
    }

    // Auto-wire Program.cs and DbContext for auditing
        await UpdateProgramForHttpContextAccessorAsync(workingDir);
        await UpdateDbContextForAuditableAsync(workingDir);

        // Update swap-config.json
        try
        {
            var (cfg, cfgPath) = Swap.CLI.Infrastructure.SwapConfigManager.LoadOrCreate(workingDir);
            Swap.CLI.Infrastructure.SwapConfigManager.RecordPattern(cfg, entity, "Auditable", new Dictionary<string, bool>{{"AuditInterceptor", true}, {"HttpContextAccessor", true}});
            Swap.CLI.Infrastructure.SwapConfigManager.Save(cfg, cfgPath);
        }
        catch { }

        AnsiConsole.MarkupLine($"[green]✓[/] Auditable pattern applied successfully!");
        AnsiConsole.MarkupLine($"[cyan]Next steps:[/]");
        AnsiConsole.MarkupLine($"  1. (Optional) Update database: [grey]dotnet ef database update[/]");

        return 0;
    }

    private static async Task<int> ApplySluggableAsync(string workingDir, string projectFile, string entity, bool force, bool usePackage, bool fallback, bool noMigrations)
    {
        // Find the entity file
        var modelPath = Path.Combine(workingDir, "Models", $"{entity}.cs");
        if (!File.Exists(modelPath))
        {
            AnsiConsole.MarkupLine($"[red]✗[/] Could not find {entity}.cs in Models folder");
            return 1;
        }

        // Parse the existing file
        var code = await File.ReadAllTextAsync(modelPath);
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetCompilationUnitRoot();

        // Check if already implements ISluggable
        var classDeclaration = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(c => c.Identifier.Text == entity);

        if (classDeclaration == null)
        {
            AnsiConsole.MarkupLine($"[red]✗[/] Could not find class {entity} in file");
            return 1;
        }

        if (classDeclaration.BaseList?.Types.Any(t => t.ToString().Contains("ISluggable")) == true)
        {
            if (!force)
            {
                AnsiConsole.MarkupLine($"[yellow]✓[/] {entity} already implements ISluggable");
                return 0;
            }
        }

        // Package mode: add using; embed mode: skip
        CompilationUnitSyntax compilationUnit = root;
        if (usePackage)
        {
            var hasUsing = root.Usings.Any(u => u.Name?.ToString() == "Swap.Patterns.Sluggable");
            if (!hasUsing)
            {
                var usingDirective = SyntaxFactory.UsingDirective(
                    SyntaxFactory.ParseName("Swap.Patterns.Sluggable"))
                    .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
                compilationUnit = compilationUnit.AddUsings(usingDirective);
            }
        }

        // Add ISluggable interface to the class
        ClassDeclarationSyntax updatedClass = classDeclaration;
        if (usePackage)
        {
            var sluggableInterface = SyntaxFactory.SimpleBaseType(
                SyntaxFactory.ParseTypeName(" ISluggable"));
            if (classDeclaration.BaseList == null)
            {
                updatedClass = classDeclaration.WithBaseList(
                    SyntaxFactory.BaseList(
                        SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(sluggableInterface)));
            }
            else
            {
                updatedClass = classDeclaration.WithBaseList(
                    classDeclaration.BaseList.AddTypes(sluggableInterface));
            }
        }

        // Check if Slug property already exists
        var hasSlugProperty = updatedClass.Members
            .OfType<PropertyDeclarationSyntax>()
            .Any(p => p.Identifier.Text == "Slug");

        if (!hasSlugProperty)
        {
            // Add Slug property
            var slugProperty = SyntaxFactory.PropertyDeclaration(
                SyntaxFactory.ParseTypeName("string"),
                "Slug")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddAccessorListAccessors(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))
                .WithInitializer(
                    SyntaxFactory.EqualsValueClause(
                        SyntaxFactory.LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            SyntaxFactory.Literal(string.Empty))))
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

            updatedClass = updatedClass.AddMembers(slugProperty);
        }

        // Replace the old class with the updated class in the compilation unit
        root = compilationUnit.ReplaceNode(
            compilationUnit.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == entity),
            updatedClass);

        // Write the updated code back to the file
        var formattedCode = root.NormalizeWhitespace().ToFullString();
        await File.WriteAllTextAsync(modelPath, formattedCode);

        AnsiConsole.MarkupLine($"[green]✓[/] Added ISluggable to {entity}");

        // Run dotnet format on the file to ensure proper formatting
        try
        {
            var formatProcess = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"format \"{projectFile}\" --include \"{modelPath}\"",
                    WorkingDirectory = workingDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            formatProcess.Start();
            await formatProcess.WaitForExitAsync();
        }
        catch
        {
            // Formatting is optional, continue even if it fails
        }

        // Auto-configure DbContext unique index for Slug
        await UpdateDbContextForSluggableAsync(workingDir, entity);

        // Auto-inject slug generation into controller actions (Create/Edit)
        await UpdateControllerForSluggableAsync(workingDir, entity);

    // Auto-create migration (do not apply update)
    if (!noMigrations)
    {
        await TryCreateMigrationAsync(workingDir, $"Add{entity}Slug");
    }
    else
    {
        AnsiConsole.MarkupLine("[yellow]ℹ[/] Migration creation skipped (--no-migrations flag)");
        AnsiConsole.MarkupLine("[dim]Create migration manually:[/] [cyan]dotnet ef migrations add Add{0}Slug[/]", entity);
    }

        return 0;
    }

    private static async Task UpdateDbContextForSluggableAsync(string workingDir, string entity)
    {
        // Find DbContext candidates
        var dataDir = Path.Combine(workingDir, "Data");
        if (!Directory.Exists(dataDir)) return;

        var candidates = Directory.GetFiles(dataDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => File.ReadAllText(f).Contains(": DbContext") || File.ReadAllText(f).Contains("IdentityDbContext"))
            .ToList();

        if (!candidates.Any())
        {
            AnsiConsole.MarkupLine("[yellow]ℹ[/] No DbContext found. Skipping DbContext configuration for Sluggable.");
            return;
        }

        string targetDbContextPath;
        if (candidates.Count == 1)
        {
            targetDbContextPath = candidates[0];
        }
        else
        {
            var choices = candidates.Select(p => Path.GetRelativePath(workingDir, p)).ToList();
            var selected = AnsiConsole.Prompt(
                new Spectre.Console.SelectionPrompt<string>()
                    .Title("Multiple DbContexts found. Select one to configure Slug index:")
                    .AddChoices(choices)
            );
            targetDbContextPath = Path.GetFullPath(Path.Combine(workingDir, selected));
        }

        var dbText = await File.ReadAllTextAsync(targetDbContextPath);
        if (dbText.Contains($"modelBuilder.Entity<{entity}>().HasIndex(e => e.Slug)"))
        {
            AnsiConsole.MarkupLine("[dim]Slug index already configured in DbContext[/]");
            return;
        }

        // Ensure OnModelCreating exists; if not, create it near the end of class
        if (!dbText.Contains("OnModelCreating(ModelBuilder modelBuilder)"))
        {
            var insertIdx = dbText.LastIndexOf('}');
            if (insertIdx > 0)
            {
                var snippet = $@"
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {{
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<{entity}>().HasIndex(e => e.Slug).IsUnique();
    }}
";
                dbText = dbText.Insert(insertIdx, snippet);
            }
        }
        else
        {
            // Insert inside existing OnModelCreating, before closing brace
            var marker = "OnModelCreating(ModelBuilder modelBuilder)";
            var start = dbText.IndexOf(marker);
            if (start >= 0)
            {
                // Find the block end
                var braceCount = 0;
                var blockStart = dbText.IndexOf('{', start);
                var i = blockStart + 1;
                for (; i < dbText.Length; i++)
                {
                    if (dbText[i] == '{') braceCount++;
                    else if (dbText[i] == '}')
                    {
                        if (braceCount == 0) break;
                        braceCount--;
                    }
                }
                if (i < dbText.Length)
                {
                    dbText = dbText.Insert(i, $"\n        modelBuilder.Entity<{entity}>().HasIndex(e => e.Slug).IsUnique();\n");
                }
            }
        }

        await File.WriteAllTextAsync(targetDbContextPath, dbText);
        AnsiConsole.MarkupLine("[green]✓[/] Configured unique Slug index in DbContext");
    }

    private static async Task UpdateDbContextForSoftDeleteAsync(string workingDir)
    {
        var dataDir = Path.Combine(workingDir, "Data");
        if (!Directory.Exists(dataDir)) return;

        var candidates = Directory.GetFiles(dataDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => File.ReadAllText(f).Contains(": DbContext") || File.ReadAllText(f).Contains("IdentityDbContext"))
            .ToList();

        if (!candidates.Any())
        {
            AnsiConsole.MarkupLine("[yellow]ℹ[/] No DbContext found. Skipping soft delete filter configuration.");
            return;
        }

        string targetDbContextPath;
        if (candidates.Count == 1)
        {
            targetDbContextPath = candidates[0];
        }
        else
        {
            var choices = candidates.Select(p => Path.GetRelativePath(workingDir, p)).ToList();
            var selected = AnsiConsole.Prompt(
                new Spectre.Console.SelectionPrompt<string>()
                    .Title("Multiple DbContexts found. Select one to configure soft delete filter:")
                    .AddChoices(choices)
            );
            targetDbContextPath = Path.GetFullPath(Path.Combine(workingDir, selected));
        }

        var dbText = await File.ReadAllTextAsync(targetDbContextPath);

        // Ensure using for extension methods
        if (!dbText.Contains("using Swap.Patterns.SoftDelete"))
        {
            dbText = "using Swap.Patterns.SoftDelete;\n" + dbText;
        }

        if (dbText.Contains("ConfigureSoftDeleteFilter()"))
        {
            AnsiConsole.MarkupLine("[dim]Soft delete global filter already configured in DbContext[/]");
        }
        else
        {
            // Ensure OnModelCreating exists; if not, create it near the end of class
            if (!dbText.Contains("OnModelCreating(ModelBuilder modelBuilder)"))
            {
                var insertIdx = dbText.LastIndexOf('}');
                if (insertIdx > 0)
                {
                    var snippet = @"
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ConfigureSoftDeleteFilter();
    }
";
                    dbText = dbText.Insert(insertIdx, snippet);
                }
            }
            else
            {
                // Insert inside existing OnModelCreating, before closing brace
                var marker = "OnModelCreating(ModelBuilder modelBuilder)";
                var start = dbText.IndexOf(marker);
                if (start >= 0)
                {
                    var braceCount = 0;
                    var blockStart = dbText.IndexOf('{', start);
                    var i = blockStart + 1;
                    for (; i < dbText.Length; i++)
                    {
                        if (dbText[i] == '{') braceCount++;
                        else if (dbText[i] == '}')
                        {
                            if (braceCount == 0) break;
                            braceCount--;
                        }
                    }
                    if (i < dbText.Length)
                    {
                        dbText = dbText.Insert(i, "\n        modelBuilder.ConfigureSoftDeleteFilter();\n");
                    }
                }
            }
        }

        await File.WriteAllTextAsync(targetDbContextPath, dbText);
        AnsiConsole.MarkupLine("[green]✓[/] Configured soft delete global filter in DbContext");
    }

    private static async Task UpdateProgramForHttpContextAccessorAsync(string workingDir)
    {
        var programPath = Path.Combine(workingDir, "Program.cs");
        if (!File.Exists(programPath))
        {
            AnsiConsole.MarkupLine("[yellow]ℹ[/] Program.cs not found. Skipping IHttpContextAccessor wiring.");
            return;
        }
        var text = await File.ReadAllTextAsync(programPath);
        if (text.Contains("AddHttpContextAccessor()")) return;

        var insertAfter = "builder.Services";
        var idx = text.IndexOf(insertAfter);
        if (idx >= 0)
        {
            var insertPos = text.IndexOf(';', idx);
            if (insertPos > 0)
            {
                text = text.Insert(insertPos + 1, "\nbuilder.Services.AddHttpContextAccessor();\n");
            }
            else
            {
                text += "\nbuilder.Services.AddHttpContextAccessor();\n";
            }
        }
        else
        {
            text += "\n// Added by swap\nbuilder.Services.AddHttpContextAccessor();\n";
        }
        await File.WriteAllTextAsync(programPath, text);
        AnsiConsole.MarkupLine("[green]✓[/] Registered IHttpContextAccessor in Program.cs");
    }

    private static async Task UpdateDbContextForAuditableAsync(string workingDir)
    {
        var dataDir = Path.Combine(workingDir, "Data");
        if (!Directory.Exists(dataDir)) return;

        var candidates = Directory.GetFiles(dataDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => File.ReadAllText(f).Contains(": DbContext") || File.ReadAllText(f).Contains("IdentityDbContext"))
            .ToList();

        if (!candidates.Any())
        {
            AnsiConsole.MarkupLine("[yellow]ℹ[/] No DbContext found. Skipping audit interceptor wiring.");
            return;
        }

        string targetDbContextPath = candidates.Count == 1
            ? candidates[0]
            : Path.GetFullPath(Path.Combine(workingDir,
                AnsiConsole.Prompt(new Spectre.Console.SelectionPrompt<string>().Title("Multiple DbContexts found. Select one to configure auditing:").AddChoices(candidates.Select(p => Path.GetRelativePath(workingDir, p))))));

        var code = await File.ReadAllTextAsync(targetDbContextPath);
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetCompilationUnitRoot();

        // 1. Add usings
        var usingsToAdd = new[] { "Swap.Patterns.Auditable", "Microsoft.AspNetCore.Http" };
        var existingUsings = root.Usings.Select(u => u.Name!.ToString()).ToHashSet();
        
        foreach (var usingName in usingsToAdd)
        {
            if (!existingUsings.Contains(usingName))
            {
                var usingDirective = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(usingName))
                    .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
                root = root.AddUsings(usingDirective);
            }
        }

        // 2. Find the DbContext class
        var classDeclaration = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(c => c.BaseList?.Types.Any(t => t.ToString().Contains("DbContext")) == true);

        if (classDeclaration == null)
        {
            AnsiConsole.MarkupLine("[yellow]ℹ[/] Could not find DbContext class. Skipping audit interceptor wiring.");
            return;
        }

        var updatedClass = classDeclaration;

        // 3. Add field if not present
        var hasField = classDeclaration.Members
            .OfType<FieldDeclarationSyntax>()
            .Any(f => f.Declaration.Variables.Any(v => v.Identifier.Text == "_httpContextAccessor"));

        if (!hasField)
        {
            var fieldDeclaration = SyntaxFactory.FieldDeclaration(
                SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.ParseTypeName("IHttpContextAccessor"))
                .WithVariables(SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.VariableDeclarator("_httpContextAccessor"))))
            .WithModifiers(
                SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                    SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)))
            .WithLeadingTrivia(SyntaxFactory.Whitespace("    "))
            .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed, SyntaxFactory.CarriageReturnLineFeed);

            updatedClass = updatedClass.WithMembers(
                updatedClass.Members.Insert(0, fieldDeclaration));
        }

        // 4. Update constructor
        var constructor = updatedClass.Members
            .OfType<ConstructorDeclarationSyntax>()
            .FirstOrDefault();

        if (constructor != null)
        {
            var hasHttpContextParam = constructor.ParameterList.Parameters
                .Any(p => p.Type?.ToString().Contains("IHttpContextAccessor") == true);

            if (!hasHttpContextParam)
            {
                // Add parameter
                var newParam = SyntaxFactory.Parameter(SyntaxFactory.Identifier("httpContextAccessor"))
                    .WithType(SyntaxFactory.ParseTypeName("IHttpContextAccessor"));

                var updatedParams = constructor.ParameterList.AddParameters(newParam);
                var updatedConstructor = constructor.WithParameterList(updatedParams);

                // Add assignment to body
                if (updatedConstructor.Body != null)
                {
                    var hasAssignment = updatedConstructor.Body.Statements
                        .OfType<ExpressionStatementSyntax>()
                        .Any(s => s.ToString().Contains("_httpContextAccessor = httpContextAccessor"));

                    if (!hasAssignment)
                    {
                        var assignment = SyntaxFactory.ExpressionStatement(
                            SyntaxFactory.AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                SyntaxFactory.IdentifierName("_httpContextAccessor"),
                                SyntaxFactory.IdentifierName("httpContextAccessor")))
                        .WithLeadingTrivia(SyntaxFactory.Whitespace("        "))
                        .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

                        updatedConstructor = updatedConstructor.WithBody(
                            updatedConstructor.Body.WithStatements(
                                updatedConstructor.Body.Statements.Insert(0, assignment)));
                    }
                }

                updatedClass = updatedClass.ReplaceNode(constructor, updatedConstructor);
            }
        }

        // 5. Add or update OnConfiguring method
        var onConfiguring = updatedClass.Members
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => m.Identifier.Text == "OnConfiguring");

        if (onConfiguring == null)
        {
            // Create new OnConfiguring method
            var methodCode = @"    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(_httpContextAccessor.CreateAuditInterceptor());
    }";
            var newMethod = SyntaxFactory.ParseMemberDeclaration(methodCode)!
                .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

            updatedClass = updatedClass.WithMembers(updatedClass.Members.Add(newMethod));
        }
        else if (onConfiguring.Body != null)
        {
            var hasInterceptor = onConfiguring.Body.Statements
                .Any(s => s.ToString().Contains("CreateAuditInterceptor"));

            if (!hasInterceptor)
            {
                var interceptorCall = SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.ParseExpression("optionsBuilder.AddInterceptors(_httpContextAccessor.CreateAuditInterceptor())"))
                .WithLeadingTrivia(SyntaxFactory.Whitespace("        "))
                .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

                var updatedMethod = onConfiguring.WithBody(
                    onConfiguring.Body.WithStatements(
                        onConfiguring.Body.Statements.Add(interceptorCall)));

                updatedClass = updatedClass.ReplaceNode(onConfiguring, updatedMethod);
            }
        }

        // Replace the class in the root
        root = root.ReplaceNode(classDeclaration, updatedClass);

        // Write back
    await File.WriteAllTextAsync(targetDbContextPath, root.NormalizeWhitespace().ToFullString());
        AnsiConsole.MarkupLine("[green]✓[/] Wired audit interceptor in DbContext");
    }

    private static async Task UpdateDbContextForTimestampableAsync(string workingDir)
    {
        var dataDir = Path.Combine(workingDir, "Data");
        if (!Directory.Exists(dataDir)) return;

        var candidates = Directory.GetFiles(dataDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => File.ReadAllText(f).Contains(": DbContext") || File.ReadAllText(f).Contains("IdentityDbContext"))
            .ToList();

        if (!candidates.Any())
        {
            AnsiConsole.MarkupLine("[yellow]ℹ[/] No DbContext found. Skipping timestamp interceptor wiring.");
            return;
        }

        string targetDbContextPath = candidates.Count == 1
            ? candidates[0]
            : Path.GetFullPath(Path.Combine(workingDir,
                AnsiConsole.Prompt(new Spectre.Console.SelectionPrompt<string>().Title("Multiple DbContexts found. Select one to configure timestamps:").AddChoices(candidates.Select(p => Path.GetRelativePath(workingDir, p))))));

        var dbText = await File.ReadAllTextAsync(targetDbContextPath);

        // Ensure usings
        if (!dbText.Contains("using Swap.Patterns.Timestampable"))
            dbText = "using Swap.Patterns.Timestampable;\n" + dbText;

        // Ensure OnConfiguring has interceptor
        if (!dbText.Contains("OnConfiguring(DbContextOptionsBuilder optionsBuilder)"))
        {
            var insertIdx = dbText.LastIndexOf('}');
            if (insertIdx > 0)
            {
                var snippet = @"
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(new TimestampInterceptor());
    }
";
                dbText = dbText.Insert(insertIdx, snippet);
            }
        }
        else if (!dbText.Contains("new TimestampInterceptor()"))
        {
            var marker = "OnConfiguring(DbContextOptionsBuilder optionsBuilder)";
            var start = dbText.IndexOf(marker);
            if (start >= 0)
            {
                var braceCount = 0;
                var blockStart = dbText.IndexOf('{', start);
                var i = blockStart + 1;
                for (; i < dbText.Length; i++)
                {
                    if (dbText[i] == '{') braceCount++;
                    else if (dbText[i] == '}')
                    {
                        if (braceCount == 0) break;
                        braceCount--;
                    }
                }
                if (i < dbText.Length)
                {
                    dbText = dbText.Insert(i, "\n        optionsBuilder.AddInterceptors(new TimestampInterceptor());\n");
                }
            }
        }

        await File.WriteAllTextAsync(targetDbContextPath, dbText);
        AnsiConsole.MarkupLine("[green]✓[/] Wired timestamp interceptor in DbContext");
    }

    private static async Task UpdateControllerForSluggableAsync(string workingDir, string entity)
    {
        var controllerPath = Path.Combine(workingDir, "Controllers", $"{entity}Controller.cs");
        if (!File.Exists(controllerPath))
        {
            AnsiConsole.MarkupLine("[yellow]ℹ[/] Controller not found to inject slug logic. Skipping.");
            return;
        }

        var content = await File.ReadAllTextAsync(controllerPath);

        // Check if helper already exists
        if (content.Contains("GenerateUniqueSlugAsync(") || content.Contains("Slugify(") || content.Contains("model.Slug ="))
        {
            AnsiConsole.MarkupLine("[dim]Slug generation already present in controller[/]");
            return;
        }

        // Try to detect a title property from the model
        var modelPath = Path.Combine(workingDir, "Models", $"{entity}.cs");
        var titleProp = "Title";
        if (File.Exists(modelPath))
        {
            var modelCode = await File.ReadAllTextAsync(modelPath);
            if (!modelCode.Contains(" Title ") && !modelCode.Contains(" Title{"))
            {
                titleProp = null;
            }
        }

        // Inject slug set before adding to context in Create action
        var addLine = $"_context.{entity}s.Add(model);";
        if (content.Contains(addLine) && titleProp != null)
        {
            content = content.Replace(addLine, $"model.Slug = await GenerateUniqueSlugAsync(model.{titleProp});\n        " + addLine);
        }

        // Inject slug regeneration before update in Edit action
        var updateLine = "_context.Update(model);";
        if (content.Contains(updateLine) && titleProp != null)
        {
            content = content.Replace(updateLine, $"model.Slug = await GenerateUniqueSlugAsync(model.{titleProp});\n            " + updateLine);
        }

        // Append helper methods at end of class
        var insertAt = content.LastIndexOf("}\n}"); // before class closing
        if (insertAt < 0) insertAt = content.LastIndexOf('}');
        if (insertAt > 0)
        {
            var helper = """

    private async Task<string> GenerateUniqueSlugAsync(string value)
    {
        var baseSlug = Slugify(value);
        var slug = baseSlug;
        var counter = 2;
        while (await _context.__ENTITY__s.AnyAsync(x => x.Slug == slug))
        {
            slug = $"{baseSlug}-{counter++}";
        }
        return slug;
    }

    private static string Slugify(string phrase)
    {
        if (string.IsNullOrWhiteSpace(phrase)) return string.Empty;
        var s = phrase.ToLowerInvariant();
        s = System.Text.RegularExpressions.Regex.Replace(s, @"[^a-z0-9\s-]", "");
        s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ").Trim();
        s = s.Replace(" ", "-");
        return s;
    }
""";
            helper = helper.Replace("__ENTITY__", entity);
            content = content.Insert(insertAt, helper);
        }

        // Ensure required using
        if (!content.Contains("using Microsoft.EntityFrameworkCore;"))
        {
            content = "using Microsoft.EntityFrameworkCore;\n" + content;
        }

        await File.WriteAllTextAsync(controllerPath, content);
        AnsiConsole.MarkupLine("[green]✓[/] Injected slug generation into controller (Create/Edit)");
    }

    // Create a migration with interactive DbContext selection; no DB update is applied
    private static async Task TryCreateMigrationAsync(string workingDir, string migrationName)
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
                ? $"ef migrations add {migrationName} --context {contextName}"
                : $"ef migrations add {migrationName}";

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

            AnsiConsole.MarkupLine($"[cyan]Creating migration:[/] {migrationName} {(contextName != null ? $"(Context: {contextName})" : string.Empty)}");
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
                    AnsiConsole.MarkupLine($"    dotnet ef migrations add {migrationName}{(contextName != null ? $" --context {contextName}" : string.Empty)}");
                }
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]⚠[/] Skipped automatic migration creation: {ex.Message}");
        }
    }

    // Heuristic finder for DbContext candidates similar to controller command
    private static List<(string className, string relativePath)> FindDbContextCandidates(string workingDir)
    {
        var results = new List<(string, string)>();
        var dataDir = Path.Combine(workingDir, "Data");
        if (!Directory.Exists(dataDir)) return results;

        foreach (var file in Directory.GetFiles(dataDir, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            var lines = text.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains(": DbContext") || line.Contains("IdentityDbContext"))
                {
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

        return results.GroupBy(r => r.Item1).Select(g => g.First()).ToList();
    }
    private static async Task<int> ApplyTimestampableAsync(string workingDir, string projectFile, string entity, bool force, bool usePackage, bool fallback, bool noMigrations)
    {
        AnsiConsole.MarkupLine($"[cyan]Adding timestampable pattern to {entity}...[/]");

        // Find entity model
        var modelPath = Path.Combine(workingDir, "Models", $"{entity}.cs");
        if (!File.Exists(modelPath))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Model file not found: {modelPath}");
            return 1;
        }

        // Ensure Swap.Patterns package reference if requested
        if (usePackage)
        {
            var packageInstalled = await EnsureSwapPatternsPackageAsync(projectFile, fallback);
            if (!packageInstalled && !fallback)
            {
                return 1;
            }
            usePackage = packageInstalled;
        }

        // Modify the entity model
        var modelContent = await File.ReadAllTextAsync(modelPath);
        
        var tree = CSharpSyntaxTree.ParseText(modelContent);
        var root = await tree.GetRootAsync();

        // Check if ITimestampable is already implemented
        var classDeclaration = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(c => c.Identifier.Text == entity);

        if (classDeclaration == null)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Could not find class {entity} in {modelPath}");
            return 1;
        }

        if (classDeclaration.BaseList?.Types.Any(t => t.ToString().Contains("ITimestampable")) ?? false)
        {
            AnsiConsole.MarkupLine($"[yellow]Warning:[/] {entity} already implements ITimestampable");
            return 0;
        }

        // Add using statement
        var hasUsingDirective = root.DescendantNodes()
            .OfType<UsingDirectiveSyntax>()
            .Any(u => u.Name?.ToString() == "Swap.Patterns.Timestampable");

        CompilationUnitSyntax newRoot = root as CompilationUnitSyntax ?? throw new InvalidOperationException();

        if (!hasUsingDirective)
        {
            var usingDirective = SyntaxFactory.UsingDirective(
                SyntaxFactory.ParseName("Swap.Patterns.Timestampable"))
                .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

            newRoot = newRoot.AddUsings(usingDirective);
        }

        // Find the class again in the new root
        classDeclaration = newRoot.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(c => c.Identifier.Text == entity);

        if (classDeclaration == null) return 1;

        // Add ITimestampable interface
        var baseList = classDeclaration.BaseList ?? SyntaxFactory.BaseList();
        var timestampableType = SyntaxFactory.SimpleBaseType(
            SyntaxFactory.ParseTypeName("ITimestampable"));

        var newBaseList = baseList.Types.Count == 0
            ? SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(timestampableType))
            : baseList.AddTypes(timestampableType);

        var newClassDeclaration = classDeclaration.WithBaseList(newBaseList);

        // Add properties if they don't exist
        var hasCreatedAt = classDeclaration.Members
            .OfType<PropertyDeclarationSyntax>()
            .Any(p => p.Identifier.Text == "CreatedAt");

        var hasUpdatedAt = classDeclaration.Members
            .OfType<PropertyDeclarationSyntax>()
            .Any(p => p.Identifier.Text == "UpdatedAt");

        if (!hasCreatedAt || !hasUpdatedAt)
        {
            var properties = new[]
            {
                ("CreatedAt", "DateTime"),
                ("UpdatedAt", "DateTime")
            };

            var newMembers = new List<MemberDeclarationSyntax>();
            for (int i = 0; i < properties.Length; i++)
            {
                var (name, type) = properties[i];
                var hasProperty = classDeclaration.Members
                    .OfType<PropertyDeclarationSyntax>()
                    .Any(p => p.Identifier.Text == name);

                if (!hasProperty)
                {
                    var prop = SyntaxFactory.PropertyDeclaration(
                        SyntaxFactory.ParseTypeName(type),
                        name)
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                        .AddAccessorListAccessors(
                            SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                            SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                        );

                    // Add comment before first property
                    if (i == 0)
                    {
                        prop = prop.WithLeadingTrivia(
                            SyntaxFactory.CarriageReturnLineFeed,
                            SyntaxFactory.Comment("    // ITimestampable properties"),
                            SyntaxFactory.CarriageReturnLineFeed
                        );
                    }

                    newMembers.Add(prop);
                }
            }

            newClassDeclaration = newClassDeclaration.AddMembers(newMembers.ToArray());
        }

        // Replace the old class with the new one
        newRoot = newRoot.ReplaceNode(classDeclaration, newClassDeclaration);

        // Write back to file with proper formatting
        var formattedCode = newRoot.NormalizeWhitespace().ToFullString();
        await File.WriteAllTextAsync(modelPath, formattedCode);

        AnsiConsole.MarkupLine($"[green]✓[/] Added ITimestampable to {entity}");

        // Auto-wire DbContext for Timestampable interceptor
        await UpdateDbContextForTimestampableAsync(workingDir);

        // Update swap-config.json
        try
        {
            var (cfg, cfgPath) = Swap.CLI.Infrastructure.SwapConfigManager.LoadOrCreate(workingDir);
            Swap.CLI.Infrastructure.SwapConfigManager.RecordPattern(cfg, entity, "Timestampable", new Dictionary<string, bool>{{"TimestampInterceptor", true}});
            Swap.CLI.Infrastructure.SwapConfigManager.Save(cfg, cfgPath);
        }
        catch { }
        
        // Create migration unless --no-migrations flag is used
        if (!noMigrations)
        {
            await TryCreateMigrationAsync(workingDir, $"AddTimestampableTo{entity}");
        }
        else
        {
            AnsiConsole.MarkupLine($"[yellow]Skipping migration creation[/] (--no-migrations flag used)");
            AnsiConsole.MarkupLine($"[grey]Create migration manually:[/] dotnet ef migrations add AddTimestampableTo{entity}");
        }

        AnsiConsole.MarkupLine($"[green]✓[/] Timestampable pattern applied successfully!");
        if (!noMigrations)
        {
            AnsiConsole.MarkupLine($"[cyan]Next steps:[/]");
            AnsiConsole.MarkupLine($"  1. (Optional) Update database: [grey]dotnet ef database update[/]");
        }
        
        return 0;
    }

    private static async Task<int> ApplyOrderableAsync(string workingDir, string projectFile, string entity, bool force, bool usePackage, bool fallback, bool noMigrations)
    {
        AnsiConsole.MarkupLine($"[cyan]Adding orderable pattern to {entity}...[/]");

        // Find entity model
        var modelPath = Path.Combine(workingDir, "Models", $"{entity}.cs");
        if (!File.Exists(modelPath))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Model file not found: {modelPath}");
            return 1;
        }

        // Ensure Swap.Patterns package reference if requested
        if (usePackage)
        {
            var packageInstalled = await EnsureSwapPatternsPackageAsync(projectFile, fallback);
            if (!packageInstalled && !fallback)
            {
                return 1;
            }
            usePackage = packageInstalled;
        }

        // Modify the entity model
        var modelContent = await File.ReadAllTextAsync(modelPath);
        var tree = CSharpSyntaxTree.ParseText(modelContent);
        var root = await tree.GetRootAsync() as CompilationUnitSyntax ?? throw new InvalidOperationException();

        // Check if class exists
        var classDeclaration = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(c => c.Identifier.Text == entity);

        if (classDeclaration == null)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Could not find class {entity} in {modelPath}");
            return 1;
        }

        // Add using Swap.Patterns.Orderable
        var hasUsing = root.DescendantNodes()
            .OfType<UsingDirectiveSyntax>()
            .Any(u => u.Name?.ToString() == "Swap.Patterns.Orderable");
        if (!hasUsing)
        {
            root = root.AddUsings(
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Swap.Patterns.Orderable"))
                    .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed)
            );
            // Refresh class reference
            classDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == entity);
        }

        // Check interface
        if (classDeclaration.BaseList?.Types.Any(t => t.ToString().Contains("IOrderable")) ?? false)
        {
            AnsiConsole.MarkupLine($"[yellow]Warning:[/] {entity} already implements IOrderable");
            return 0;
        }

        // Add interface to base list
        var baseList = classDeclaration.BaseList ?? SyntaxFactory.BaseList();
        var orderableType = SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("IOrderable"));
        var newBaseList = baseList.Types.Count == 0
            ? SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(orderableType))
            : baseList.AddTypes(orderableType);
        var newClassDeclaration = classDeclaration.WithBaseList(newBaseList);

        // Add Position property if missing
        var hasPosition = classDeclaration.Members
            .OfType<PropertyDeclarationSyntax>()
            .Any(p => p.Identifier.Text == "Position");
        if (!hasPosition)
        {
            var prop = SyntaxFactory.PropertyDeclaration(
                    SyntaxFactory.ParseTypeName("int"),
                    "Position")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddAccessorListAccessors(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                )
                .WithLeadingTrivia(
                    SyntaxFactory.CarriageReturnLineFeed,
                    SyntaxFactory.Comment("    // IOrderable properties"),
                    SyntaxFactory.CarriageReturnLineFeed
                );

            newClassDeclaration = newClassDeclaration.AddMembers(prop);
        }

        // Replace and write
        var newRoot = root.ReplaceNode(classDeclaration, newClassDeclaration);
        var formattedCode = newRoot.NormalizeWhitespace().ToFullString();
        await File.WriteAllTextAsync(modelPath, formattedCode);

        AnsiConsole.MarkupLine($"[green]✓[/] Added IOrderable to {entity}");
        
        // Create migration unless --no-migrations flag is used
        if (!noMigrations)
        {
            await TryCreateMigrationAsync(workingDir, $"AddOrderableTo{entity}");
        }
        else
        {
            AnsiConsole.MarkupLine($"[yellow]Skipping migration creation[/] (--no-migrations flag used)");
            AnsiConsole.MarkupLine($"[grey]Create migration manually:[/] dotnet ef migrations add AddOrderableTo{entity}");
        }
        
        AnsiConsole.MarkupLine($"\n[yellow]Tips:[/]");
        AnsiConsole.MarkupLine($"- Use OrderByPosition() to sort lists by Position");
        AnsiConsole.MarkupLine($"- Use GetNextPositionAsync() for assigning a Position on create");
        AnsiConsole.MarkupLine($"- Use ReorderAsync() to move items and NormalizePositionsAsync() after deletes");

        if (noMigrations)
        {
            AnsiConsole.MarkupLine($"\n2. Create and run migration if new property added:");
            AnsiConsole.MarkupLine($"   [grey]dotnet ef migrations add AddOrderableTo{entity}[/]");
            AnsiConsole.MarkupLine($"   [grey]dotnet ef database update[/]");
        }

        return 0;
    }

    private static async Task<bool> EnsureSwapPatternsPackageAsync(string projectFile, bool fallback)
    {
        var content = await File.ReadAllTextAsync(projectFile);
        if (content.Contains("Swap.Patterns"))
        {
            return true;
        }

        var projectDir = Path.GetDirectoryName(projectFile)!;

        // Always prefer NuGet package for external developers
        AnsiConsole.MarkupLine($"[cyan]Installing Swap.Patterns package...[/]");

        var processInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "add package Swap.Patterns --prerelease",
            WorkingDirectory = projectDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = System.Diagnostics.Process.Start(processInfo);
        if (process != null)
        {
            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
            {
                if (fallback)
                {
                    AnsiConsole.MarkupLine($"[yellow]Warning:[/] Failed to install Swap.Patterns package. Falling back to embedded code.");
                    return false;
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]Error:[/] Failed to install Swap.Patterns package.");
                    AnsiConsole.MarkupLine($"[yellow]Run manually:[/] [grey]dotnet add package Swap.Patterns --prerelease[/]");
                    AnsiConsole.MarkupLine($"[yellow]Or use:[/] [grey]--fallback[/] to embed code instead");
                    return false;
                }
            }
            else
            {
                AnsiConsole.MarkupLine($"[green]✓[/] Swap.Patterns package added");
                return true;
            }
        }
        
        return false;
    }

    private static async Task<int> ApplyPublishableAsync(string workingDir, string projectFile, string entity, bool force, bool usePackage, bool fallback, bool noMigrations)
    {
        AnsiConsole.MarkupLine($"[cyan]Adding publishable pattern to {entity}...[/]");

        // Find entity model
        var modelPath = Path.Combine(workingDir, "Models", $"{entity}.cs");
        if (!File.Exists(modelPath))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Model file not found: {modelPath}");
            return 1;
        }

        // Ensure Swap.Patterns package reference if requested
        if (usePackage)
        {
            var packageInstalled = await EnsureSwapPatternsPackageAsync(projectFile, fallback);
            if (!packageInstalled && !fallback)
            {
                return 1;
            }
            usePackage = packageInstalled;
        }

        // Modify the entity model
        var code = await File.ReadAllTextAsync(modelPath);
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync() as CompilationUnitSyntax ?? throw new InvalidOperationException();

        // Locate class
        var classDecl = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault(c => c.Identifier.Text == entity);
        if (classDecl == null)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Class {entity} not found in {modelPath}");
            return 1;
        }

        // Add using Swap.Patterns.Publishable
        var hasUsing = root.DescendantNodes().OfType<UsingDirectiveSyntax>().Any(u => u.Name?.ToString() == "Swap.Patterns.Publishable");
        if (!hasUsing)
        {
            root = root.AddUsings(
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Swap.Patterns.Publishable"))
                    .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed)
            );
            classDecl = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == entity);
        }

        // Ensure it implements IPublishable
        if (!(classDecl.BaseList?.Types.Any(t => t.ToString().Contains("IPublishable")) ?? false))
        {
            var baseList = classDecl.BaseList ?? SyntaxFactory.BaseList();
            var publishableType = SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("IPublishable"));
            var newBaseList = baseList.Types.Count == 0
                ? SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(publishableType))
                : baseList.AddTypes(publishableType);
            classDecl = classDecl.WithBaseList(newBaseList);
        }

        // Add properties if missing
        bool hasIsPublished = classDecl.Members.OfType<PropertyDeclarationSyntax>().Any(p => p.Identifier.Text == "IsPublished");
        bool hasPublishedAt = classDecl.Members.OfType<PropertyDeclarationSyntax>().Any(p => p.Identifier.Text == "PublishedAt");

        var newMembers = new List<MemberDeclarationSyntax>();
        if (!hasIsPublished)
        {
            var prop = SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("bool"), "IsPublished")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddAccessorListAccessors(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                )
                .WithLeadingTrivia(
                    SyntaxFactory.CarriageReturnLineFeed,
                    SyntaxFactory.Comment("    // IPublishable properties"),
                    SyntaxFactory.CarriageReturnLineFeed
                );
            newMembers.Add(prop);
        }
        if (!hasPublishedAt)
        {
            var prop = SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("DateTime?"), "PublishedAt")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddAccessorListAccessors(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                );
            newMembers.Add(prop);
        }
        if (newMembers.Count > 0)
        {
            classDecl = classDecl.AddMembers(newMembers.ToArray());
        }

        // Replace node and write back
        root = root.ReplaceNode(root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == entity), classDecl);
        var formatted = root.NormalizeWhitespace().ToFullString();
        await File.WriteAllTextAsync(modelPath, formatted);

        AnsiConsole.MarkupLine($"[green]✓[/] Added IPublishable to {entity}");
        
        // Create migration unless --no-migrations flag is used
        if (!noMigrations)
        {
            await TryCreateMigrationAsync(workingDir, $"AddPublishableTo{entity}");
        }
        else
        {
            AnsiConsole.MarkupLine($"[yellow]Skipping migration creation[/] (--no-migrations flag used)");
            AnsiConsole.MarkupLine($"[grey]Create migration manually:[/] dotnet ef migrations add AddPublishableTo{entity}");
        }
        
        AnsiConsole.MarkupLine($"\n[yellow]Tips:[/]");
        AnsiConsole.MarkupLine($"- Use .Publish() / .Unpublish() helpers");
        AnsiConsole.MarkupLine($"- Filter with .Published() / .Drafts() queries");
        
        if (noMigrations)
        {
            AnsiConsole.MarkupLine($"\n[yellow]Next steps:[/]");
            AnsiConsole.MarkupLine($"1. Add migration if new properties were added:");
            AnsiConsole.MarkupLine($"   [grey]dotnet ef migrations add AddPublishableTo{entity}[/]");
            AnsiConsole.MarkupLine($"   [grey]dotnet ef database update[/]");
        }

        return 0;
    }

    private static async Task<int> ApplyVersionableAsync(string workingDir, string projectFile, string entity, bool force, bool usePackage, bool fallback, bool noMigrations)
    {
        AnsiConsole.MarkupLine($"[cyan]Adding versionable pattern to {entity}...[/]");

        // Find entity model
        var modelPath = Path.Combine(workingDir, "Models", $"{entity}.cs");
        if (!File.Exists(modelPath))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Model file not found: {modelPath}");
            return 1;
        }

        // Ensure Swap.Patterns package reference if requested
        if (usePackage)
        {
            var packageInstalled = await EnsureSwapPatternsPackageAsync(projectFile, fallback);
            if (!packageInstalled && !fallback)
            {
                return 1;
            }
            usePackage = packageInstalled;
        }

        // Modify the entity model
        var code = await File.ReadAllTextAsync(modelPath);
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync() as CompilationUnitSyntax ?? throw new InvalidOperationException();

        // Locate class
        var classDecl = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault(c => c.Identifier.Text == entity);
        if (classDecl == null)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Class {entity} not found in {modelPath}");
            return 1;
        }

        // Add using Swap.Patterns.Versionable
        var hasUsing = root.DescendantNodes().OfType<UsingDirectiveSyntax>().Any(u => u.Name?.ToString() == "Swap.Patterns.Versionable");
        if (!hasUsing)
        {
            root = root.AddUsings(
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Swap.Patterns.Versionable"))
                    .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed)
            );
            classDecl = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == entity);
        }

        // Ensure it implements IVersionable
        if (!(classDecl.BaseList?.Types.Any(t => t.ToString().Contains("IVersionable")) ?? false))
        {
            var baseList = classDecl.BaseList ?? SyntaxFactory.BaseList();
            var versionableType = SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("IVersionable"));
            var newBaseList = baseList.Types.Count == 0
                ? SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(versionableType))
                : baseList.AddTypes(versionableType);
            classDecl = classDecl.WithBaseList(newBaseList);
        }

        // Add Version property if missing
        bool hasVersion = classDecl.Members.OfType<PropertyDeclarationSyntax>().Any(p => p.Identifier.Text == "Version");
        if (!hasVersion)
        {
            var prop = SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("int"), "Version")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddAccessorListAccessors(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                )
                .WithLeadingTrivia(
                    SyntaxFactory.CarriageReturnLineFeed,
                    SyntaxFactory.Comment("    // IVersionable properties"),
                    SyntaxFactory.CarriageReturnLineFeed
                );
            classDecl = classDecl.AddMembers(prop);
        }

        // Replace node and write back
        root = root.ReplaceNode(root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == entity), classDecl);
        var formatted = root.NormalizeWhitespace().ToFullString();
        await File.WriteAllTextAsync(modelPath, formatted);

        AnsiConsole.MarkupLine($"[green]✓[/] Added IVersionable to {entity}");
        
        // Create migration unless --no-migrations flag is used
        if (!noMigrations)
        {
            await TryCreateMigrationAsync(workingDir, $"AddVersionableTo{entity}");
        }
        else
        {
            AnsiConsole.MarkupLine($"[yellow]Skipping migration creation[/] (--no-migrations flag used)");
            AnsiConsole.MarkupLine($"[grey]Create migration manually:[/] dotnet ef migrations add AddVersionableTo{entity}");
        }
        
        AnsiConsole.MarkupLine($"\n[yellow]Next steps:[/]");
        AnsiConsole.MarkupLine($"1. Add the interceptor to your DbContext:");
        AnsiConsole.MarkupLine($"   [grey]protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)[/]");
        AnsiConsole.MarkupLine($"   [grey]{{[/]");
        AnsiConsole.MarkupLine($"   [grey]    optionsBuilder.AddInterceptors(new VersionInterceptor());[/]");
        AnsiConsole.MarkupLine($"   [grey]}}[/]");
        
        if (noMigrations)
        {
            AnsiConsole.MarkupLine($"\n2. Create and run migration:");
            AnsiConsole.MarkupLine($"   [grey]dotnet ef migrations add AddVersionableTo{entity}[/]");
            AnsiConsole.MarkupLine($"   [grey]dotnet ef database update[/]");
        }

        return 0;
    }

    private static async Task<int> ApplyVisibilityAsync(string workingDir, string projectFile, string entity, bool force, bool usePackage, bool fallback, bool noMigrations)
    {
        AnsiConsole.MarkupLine($"[cyan]Adding visibility pattern to {entity}...[/]");

        // Find entity model
        var modelPath = Path.Combine(workingDir, "Models", $"{entity}.cs");
        if (!File.Exists(modelPath))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Model file not found: {modelPath}");
            return 1;
        }

        // Ensure Swap.Patterns package reference if requested
        if (usePackage)
        {
            var packageInstalled = await EnsureSwapPatternsPackageAsync(projectFile, fallback);
            if (!packageInstalled && !fallback)
            {
                return 1;
            }
            usePackage = packageInstalled;
        }

        // Modify the entity model
        var code = await File.ReadAllTextAsync(modelPath);
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync() as CompilationUnitSyntax ?? throw new InvalidOperationException();

        // Locate class
        var classDecl = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault(c => c.Identifier.Text == entity);
        if (classDecl == null)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Class {entity} not found in {modelPath}");
            return 1;
        }

        // Add using Swap.Patterns.Visibility
        var hasUsing = root.DescendantNodes().OfType<UsingDirectiveSyntax>().Any(u => u.Name?.ToString() == "Swap.Patterns.Visibility");
        if (!hasUsing)
        {
            root = root.AddUsings(
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Swap.Patterns.Visibility"))
                    .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed)
            );
            classDecl = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == entity);
        }

        // Ensure it implements IVisibility
        if (!(classDecl.BaseList?.Types.Any(t => t.ToString().Contains("IVisibility")) ?? false))
        {
            var baseList = classDecl.BaseList ?? SyntaxFactory.BaseList();
            var visibilityType = SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("IVisibility"));
            var newBaseList = baseList.Types.Count == 0
                ? SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(visibilityType))
                : baseList.AddTypes(visibilityType);
            classDecl = classDecl.WithBaseList(newBaseList);
        }

        // Add properties if missing
        bool hasIsVisible = classDecl.Members.OfType<PropertyDeclarationSyntax>().Any(p => p.Identifier.Text == "IsVisible");
        bool hasVisibleFrom = classDecl.Members.OfType<PropertyDeclarationSyntax>().Any(p => p.Identifier.Text == "VisibleFrom");
        bool hasVisibleUntil = classDecl.Members.OfType<PropertyDeclarationSyntax>().Any(p => p.Identifier.Text == "VisibleUntil");

        var newMembers = new List<MemberDeclarationSyntax>();
        if (!hasIsVisible)
        {
            var prop = SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("bool"), "IsVisible")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddAccessorListAccessors(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                )
                .WithLeadingTrivia(
                    SyntaxFactory.CarriageReturnLineFeed,
                    SyntaxFactory.Comment("    // IVisibility properties"),
                    SyntaxFactory.CarriageReturnLineFeed
                );
            newMembers.Add(prop);
        }
        if (!hasVisibleFrom)
        {
            var prop = SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("DateTime?"), "VisibleFrom")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddAccessorListAccessors(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                );
            newMembers.Add(prop);
        }
        if (!hasVisibleUntil)
        {
            var prop = SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("DateTime?"), "VisibleUntil")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddAccessorListAccessors(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                );
            newMembers.Add(prop);
        }
        if (newMembers.Count > 0)
        {
            classDecl = classDecl.AddMembers(newMembers.ToArray());
        }

        // Replace node and write back
        root = root.ReplaceNode(root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == entity), classDecl);
        var formatted = root.NormalizeWhitespace().ToFullString();
        await File.WriteAllTextAsync(modelPath, formatted);

        AnsiConsole.MarkupLine($"[green]✓[/] Added IVisibility to {entity}");
        
        // Create migration unless --no-migrations flag is used
        if (!noMigrations)
        {
            await TryCreateMigrationAsync(workingDir, $"AddVisibilityTo{entity}");
        }
        else
        {
            AnsiConsole.MarkupLine($"[yellow]Skipping migration creation[/] (--no-migrations flag used)");
            AnsiConsole.MarkupLine($"[grey]Create migration manually:[/] dotnet ef migrations add AddVisibilityTo{entity}");
        }
        
        AnsiConsole.MarkupLine($"\n[yellow]Tips:[/]");
        AnsiConsole.MarkupLine($"- Use .Show() / .Hide() to toggle visibility manually");
        AnsiConsole.MarkupLine($"- Use .ScheduleVisibility(dateUtc) for future visibility");
        AnsiConsole.MarkupLine($"- Use .ScheduleVisibilityWindow(from, until) for time-bound content");
        AnsiConsole.MarkupLine($"- Use .IsCurrentlyVisible() or .Visible() query helper to filter visible items");

        return 0;
    }

    private static async Task<string?> CheckPatternCompatibilityAsync(string modelPath, string entity, string patternType)
    {
        var code = await File.ReadAllTextAsync(modelPath);
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetCompilationUnitRoot();

        var classDecl = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(c => c.Identifier.Text == entity);

        if (classDecl == null) return null;

        var existingInterfaces = classDecl.BaseList?.Types
            .Select(t => t.ToString().Trim())
            .Where(t => t.StartsWith("I"))
            .ToHashSet() ?? new HashSet<string>();

        var normalizedPattern = patternType.ToLower();

        // Define incompatibility matrix
        var conflicts = new Dictionary<string, (string[] incompatible, string reason)>
        {
            ["auditable"] = (
                new[] { "ITimestampable" },
                "IAuditable and ITimestampable both manage UpdatedAt but with different types (DateTime? vs DateTime). Choose one based on your needs:\n" +
                "  - IAuditable: Tracks WHO changed the entity (user tracking)\n" +
                "  - ITimestampable: Only tracks WHEN (simpler, no user context)"
            ),
            ["timestamp"] = (
                new[] { "IAuditable" },
                "ITimestampable and IAuditable both manage UpdatedAt but with different types (DateTime vs DateTime?). Choose one based on your needs:\n" +
                "  - IAuditable: Tracks WHO changed the entity (user tracking)\n" +
                "  - ITimestampable: Only tracks WHEN (simpler, no user context)"
            ),
            ["timestampable"] = (
                new[] { "IAuditable" },
                "ITimestampable and IAuditable both manage UpdatedAt but with different types (DateTime vs DateTime?). Choose one based on your needs:\n" +
                "  - IAuditable: Tracks WHO changed the entity (user tracking)\n" +
                "  - ITimestampable: Only tracks WHEN (simpler, no user context)"
            ),
            ["audit"] = (
                new[] { "ITimestampable" },
                "IAuditable and ITimestampable both manage UpdatedAt but with different types (DateTime? vs DateTime). Choose one based on your needs:\n" +
                "  - IAuditable: Tracks WHO changed the entity (user tracking)\n" +
                "  - ITimestampable: Only tracks WHEN (simpler, no user context)"
            )
        };

        if (conflicts.TryGetValue(normalizedPattern, out var conflict))
        {
            foreach (var incompatibleInterface in conflict.incompatible)
            {
                if (existingInterfaces.Any(i => i.Contains(incompatibleInterface)))
                {
                    return $"{entity} already implements {incompatibleInterface}.\n{conflict.reason}";
                }
            }
        }

        return null;
    }

}
