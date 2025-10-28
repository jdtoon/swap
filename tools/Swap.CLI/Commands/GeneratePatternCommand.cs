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
    var command = new Command("pattern", "Add common patterns to entities (softdelete, auditable, sluggable, timestampable, orderable)");
        command.AddAlias("p");

        var typeArg = new Argument<string>(
            name: "type",
            description: "Pattern type: softdelete, auditable, sluggable, timestampable, or orderable");

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
                "timestampable" or "timestamp" => await ApplyTimestampableAsync(workingDir, projectFile, entity, force),
                "orderable" or "order" => await ApplyOrderableAsync(workingDir, projectFile, entity, force),
                _ => throw new Exception($"Unknown pattern type: {type}. Use softdelete, auditable, sluggable, timestampable, or orderable")
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

        // Write back with normalization
        var formattedCode = root.NormalizeWhitespace().ToFullString();
        await File.WriteAllTextAsync(modelPath, formattedCode);

        AnsiConsole.MarkupLine($"[green]✓[/] Added ISoftDeletable to {entity}");

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
            // Formatting is optional, continue if it fails
        }

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
        AnsiConsole.MarkupLine($"[cyan]Adding auditable pattern to {entity}...[/]");

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

        // Add using statement
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

        // Add IAuditable interface
        var newBaseList = classDecl.BaseList ?? SyntaxFactory.BaseList();
        newBaseList = newBaseList.AddTypes(
            SyntaxFactory.SimpleBaseType(
                SyntaxFactory.ParseTypeName(" IAuditable")));

        classDecl = classDecl.WithBaseList(newBaseList);

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

                    // Add comment before first property
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

        // Replace the class in the tree
        root = root.ReplaceNode(root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == entity), classDecl);

        // Write back with normalization
        var formattedCode = root.NormalizeWhitespace().ToFullString();
        await File.WriteAllTextAsync(modelPath, formattedCode);

        AnsiConsole.MarkupLine($"[green]✓[/] Added IAuditable to {entity}");

        // Run dotnet format
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
            // Formatting is optional
        }

        // Provide configuration guidance
        AnsiConsole.MarkupLine($"[yellow]Note:[/] Configure the audit interceptor in your DbContext:");
        AnsiConsole.MarkupLine($"[grey]// In Program.cs[/]");
        AnsiConsole.MarkupLine($"[grey]builder.Services.AddHttpContextAccessor();[/]");
        AnsiConsole.MarkupLine($"");
        AnsiConsole.MarkupLine($"[grey]// In your DbContext constructor[/]");
        AnsiConsole.MarkupLine($"[grey]private readonly IHttpContextAccessor _httpContextAccessor;[/]");
        AnsiConsole.MarkupLine($"");
        AnsiConsole.MarkupLine($"[grey]public AppDbContext(DbContextOptions options, IHttpContextAccessor httpContextAccessor)[/]");
        AnsiConsole.MarkupLine($"[grey]    : base(options)[/]");
        AnsiConsole.MarkupLine($"[grey]{{[/]");
        AnsiConsole.MarkupLine($"[grey]    _httpContextAccessor = httpContextAccessor;[/]");
        AnsiConsole.MarkupLine($"[grey]}}[/]");
        AnsiConsole.MarkupLine($"");
        AnsiConsole.MarkupLine($"[grey]// In OnConfiguring[/]");
        AnsiConsole.MarkupLine($"[grey]protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)[/]");
        AnsiConsole.MarkupLine($"[grey]{{[/]");
        AnsiConsole.MarkupLine($"[grey]    optionsBuilder.AddInterceptors(_httpContextAccessor.CreateAuditInterceptor());[/]");
        AnsiConsole.MarkupLine($"[grey]}}[/]");

        AnsiConsole.MarkupLine($"[green]✓[/] Auditable pattern applied successfully!");
        AnsiConsole.MarkupLine($"[cyan]Next steps:[/]");
        AnsiConsole.MarkupLine($"  1. Add migration: [grey]dotnet ef migrations add AddAuditableTo{entity}[/]");
        AnsiConsole.MarkupLine($"  2. Update database: [grey]dotnet ef database update[/]");

        return 0;
    }

    private static async Task<int> ApplySluggableAsync(string workingDir, string projectFile, string entity, bool force)
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

        // Add using statement for Swap.Patterns.Sluggable
        var hasUsing = root.Usings.Any(u => u.Name?.ToString() == "Swap.Patterns.Sluggable");
        CompilationUnitSyntax compilationUnit = root;
        if (!hasUsing)
        {
            var usingDirective = SyntaxFactory.UsingDirective(
                SyntaxFactory.ParseName("Swap.Patterns.Sluggable"))
                .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
            compilationUnit = compilationUnit.AddUsings(usingDirective);
        }

        // Add ISluggable interface to the class
        var sluggableInterface = SyntaxFactory.SimpleBaseType(
            SyntaxFactory.ParseTypeName(" ISluggable"));

        ClassDeclarationSyntax updatedClass;
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

        // Output configuration guidance
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold cyan]Configuration Steps:[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]1. Add unique index in your DbContext OnModelCreating:[/]");
        AnsiConsole.MarkupLine("[grey]   protected override void OnModelCreating(ModelBuilder modelBuilder)[/]");
        AnsiConsole.MarkupLine("[grey]   {[/]");
        AnsiConsole.MarkupLine("[grey]       modelBuilder.ConfigureSlugIndexes();[/]");
        AnsiConsole.MarkupLine("[grey]       // OR manually for specific entity:[/]");
        AnsiConsole.MarkupLine($"[grey]       modelBuilder.Entity<{entity}>()[/]");
        AnsiConsole.MarkupLine("[grey]           .HasIndex(e => e.Slug)[/]");
        AnsiConsole.MarkupLine("[grey]           .IsUnique();[/]");
        AnsiConsole.MarkupLine("[grey]   }[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]2. Generate slug before saving (in your controller or service):[/]");
        AnsiConsole.MarkupLine($"[grey]   var {entity.ToLower()} = new {entity} {{ /* properties */ }};[/]");
        AnsiConsole.MarkupLine($"[grey]   await {entity.ToLower()}.GenerateSlugAsync({entity.ToLower()}.Title, _db);[/]");
        AnsiConsole.MarkupLine($"[grey]   _db.{entity}s.Add({entity.ToLower()});[/]");
        AnsiConsole.MarkupLine("[grey]   await _db.SaveChangesAsync();[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]3. Add a migration:[/]");
        AnsiConsole.MarkupLine($"[grey]   dotnet ef migrations add Add{entity}Slug[/]");
        AnsiConsole.MarkupLine("[grey]   dotnet ef database update[/]");
        AnsiConsole.WriteLine();

        // Ensure Swap.Patterns package
        await EnsureSwapPatternsPackageAsync(projectFile);

        return 0;
    }
    private static async Task<int> ApplyTimestampableAsync(string workingDir, string projectFile, string entity, bool force)
    {
        AnsiConsole.MarkupLine($"[cyan]Adding timestampable pattern to {entity}...[/]");

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
        AnsiConsole.MarkupLine($"\n[yellow]Next steps:[/]");
        AnsiConsole.MarkupLine($"1. Add the interceptor to your DbContext:");
        AnsiConsole.MarkupLine($"   [grey]protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)[/]");
        AnsiConsole.MarkupLine($"   [grey]{{[/]");
        AnsiConsole.MarkupLine($"   [grey]    optionsBuilder.AddInterceptors(new TimestampInterceptor());[/]");
        AnsiConsole.MarkupLine($"   [grey]}}[/]");
        AnsiConsole.MarkupLine($"\n2. Create and run migration:");
        AnsiConsole.MarkupLine($"   [grey]dotnet ef migrations add AddTimestampableTo{entity}[/]");
        AnsiConsole.MarkupLine($"   [grey]dotnet ef database update[/]");

        return 0;
    }

    private static async Task<int> ApplyOrderableAsync(string workingDir, string projectFile, string entity, bool force)
    {
        AnsiConsole.MarkupLine($"[cyan]Adding orderable pattern to {entity}...[/]");

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
        AnsiConsole.MarkupLine($"\n[yellow]Tips:[/]");
        AnsiConsole.MarkupLine($"- Use OrderByPosition() to sort lists by Position");
        AnsiConsole.MarkupLine($"- Use GetNextPositionAsync() for assigning a Position on create");
        AnsiConsole.MarkupLine($"- Use ReorderAsync() to move items and NormalizePositionsAsync() after deletes");

        AnsiConsole.MarkupLine($"\n2. Create and run migration if new property added:");
        AnsiConsole.MarkupLine($"   [grey]dotnet ef migrations add AddOrderableTo{entity}[/]");
        AnsiConsole.MarkupLine($"   [grey]dotnet ef database update[/]");

        return 0;
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
