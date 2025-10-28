using System.CommandLine;
using System.CommandLine.Invocation;
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
        var command = new Command("pattern", "Add common patterns to entities (softdelete, auditable, sluggable)");
        command.AddAlias("p");

        var typeArg = new Argument<string>(
            name: "type",
            description: "Pattern type: softdelete, auditable, or sluggable");

        var entityArg = new Argument<string>(
            name: "entity",
            description: "Entity name (e.g., Post)");

        var forceOption = new Option<bool>(
            aliases: new[] { "--force", "-f" },
            description: "Overwrite files without prompting");

        var projectOption = new Option<string?>(
            aliases: new[] { "--project", "-p" },
            description: "Path to project directory (default: current directory)");

        command.AddArgument(typeArg);
        command.AddArgument(entityArg);
        command.AddOption(forceOption);
        command.AddOption(projectOption);

        command.SetHandler(async (InvocationContext ctx) =>
        {
            var type = ctx.ParseResult.GetValueForArgument(typeArg);
            var entity = ctx.ParseResult.GetValueForArgument(entityArg);
            var force = ctx.ParseResult.GetValueForOption(forceOption);
            var projectPath = ctx.ParseResult.GetValueForOption(projectOption);

            ctx.ExitCode = await ExecuteAsync(type, entity, force, projectPath);
        });

        return command;
    }

    public static async Task<int> ExecuteAsync(string type, string entity, bool force, string? projectPath)
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

        try
        {
            return type.ToLower() switch
            {
                "softdelete" or "soft" => await ApplySoftDeleteAsync(workingDir, projectFile, entity, force),
                "auditable" or "audit" => await ApplyAuditableAsync(workingDir, projectFile, entity, force),
                "sluggable" or "slug" => await ApplySluggableAsync(workingDir, projectFile, entity, force),
                _ => throw new Exception($"Unknown pattern type: {type}. Use softdelete, auditable, or sluggable")
            };
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> ApplySoftDeleteAsync(string workingDir, string projectFile, string entity, bool force)
    {
        AnsiConsole.MarkupLine($"[cyan]Adding soft delete pattern to {entity}...[/]");

        // Find entity model
        var modelPath = Path.Combine(workingDir, "Models", $"{entity}.cs");
        if (!File.Exists(modelPath))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Model file not found: {modelPath}");
            return 1;
        }

        // Ensure Swap.Patterns package reference
        await EnsureSwapPatternsPackageAsync(projectFile);

        // Modify the entity model
        var modelContent = await File.ReadAllTextAsync(modelPath);
        var tree = CSharpSyntaxTree.ParseText(modelContent);
        var root = await tree.GetRootAsync();

        // Check if already implements ISoftDeletable
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

        // Add using statement
        var hasUsing = root.DescendantNodes()
            .OfType<UsingDirectiveSyntax>()
            .Any(u => u.Name?.ToString() == "Swap.Patterns.SoftDelete");

        CompilationUnitSyntax compilationUnit = (CompilationUnitSyntax)root;
        if (!hasUsing)
        {
            var usingDirective = SyntaxFactory.UsingDirective(
                SyntaxFactory.ParseName("Swap.Patterns.SoftDelete"))
                .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
            compilationUnit = compilationUnit.AddUsings(usingDirective);
            root = compilationUnit;
        }

        // Add ISoftDeletable interface with proper formatting
        var newBaseList = classDecl.BaseList ?? SyntaxFactory.BaseList();
        newBaseList = newBaseList.AddTypes(
            SyntaxFactory.SimpleBaseType(
                SyntaxFactory.ParseTypeName(" ISoftDeletable")));

        classDecl = classDecl.WithBaseList(newBaseList);

        // Add properties if they don't exist with a comment
        var hasIsDeleted = classDecl.Members
            .OfType<PropertyDeclarationSyntax>()
            .Any(p => p.Identifier.Text == "IsDeleted");

        if (!hasIsDeleted)
        {
            var properties = new[]
            {
                ("IsDeleted", "bool"),
                ("DeletedAt", "DateTime?"),
                ("DeletedBy", "string?")
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

                    // Add comment before first property
                    if (i == 0)
                    {
                        prop = prop.WithLeadingTrivia(
                            SyntaxFactory.CarriageReturnLineFeed,
                            SyntaxFactory.Comment("    // ISoftDeletable properties"),
                            SyntaxFactory.CarriageReturnLineFeed
                        );
                    }

                    newMembers.Add(prop);
                }
            }

            classDecl = classDecl.AddMembers(newMembers.ToArray());
        }

        // Replace the class in the tree
        root = root.ReplaceNode(root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == entity), classDecl);

        // Write back
        await File.WriteAllTextAsync(modelPath, root.ToFullString());

        AnsiConsole.MarkupLine($"[green]✓[/] Added ISoftDeletable to {entity}");

        // Check DbContext and offer to add query filter
        var dbContextPath = Path.Combine(workingDir, "Data", "AppDbContext.cs");
        if (File.Exists(dbContextPath))
        {
            var dbContent = await File.ReadAllTextAsync(dbContextPath);
            if (!dbContent.Contains("ConfigureSoftDeleteFilter"))
            {
                AnsiConsole.MarkupLine($"[yellow]Note:[/] Add the following to your AppDbContext.OnModelCreating:");
                AnsiConsole.MarkupLine($"[grey]modelBuilder.ConfigureSoftDeleteFilter();[/]");
            }
        }

        AnsiConsole.MarkupLine($"[green]✓[/] Soft delete pattern applied successfully!");
        AnsiConsole.MarkupLine($"[cyan]Next steps:[/]");
        AnsiConsole.MarkupLine($"  1. Add migration: [grey]dotnet ef migrations add AddSoftDeleteTo{entity}[/]");
        AnsiConsole.MarkupLine($"  2. Update database: [grey]dotnet ef database update[/]");

        return 0;
    }

    private static async Task<int> ApplyAuditableAsync(string workingDir, string projectFile, string entity, bool force)
    {
        AnsiConsole.MarkupLine($"[yellow]Auditable pattern coming soon![/]");
        return 1;
    }

    private static async Task<int> ApplySluggableAsync(string workingDir, string projectFile, string entity, bool force)
    {
        AnsiConsole.MarkupLine($"[yellow]Sluggable pattern coming soon![/]");
        return 1;
    }

    private static async Task EnsureSwapPatternsPackageAsync(string projectFile)
    {
        var content = await File.ReadAllTextAsync(projectFile);
        if (content.Contains("Swap.Patterns"))
        {
            return;
        }

        AnsiConsole.MarkupLine($"[cyan]Installing Swap.Patterns package...[/]");
        var projectDir = Path.GetDirectoryName(projectFile)!;

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
                AnsiConsole.MarkupLine($"[yellow]Warning:[/] Failed to add Swap.Patterns package automatically.");
                AnsiConsole.MarkupLine($"[yellow]Run manually:[/] [grey]dotnet add package Swap.Patterns --prerelease[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[green]✓[/] Swap.Patterns package added");
            }
        }
    }
}
