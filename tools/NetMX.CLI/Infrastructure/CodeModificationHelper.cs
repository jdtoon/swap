using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using System.Text;

namespace NetMX.CLI.Infrastructure;

/// <summary>
/// Helper class for modifying C# code using Roslyn API.
/// Provides safe, formatted code modifications with error handling.
/// </summary>
public class CodeModificationHelper
{
    /// <summary>
    /// Adds a DbSet property to a DbContext class.
    /// </summary>
    /// <param name="sourceCode">The original C# source code</param>
    /// <param name="entityName">The entity name (e.g., "Product")</param>
    /// <param name="entityNamespace">The full namespace of the entity (optional)</param>
    /// <returns>Modified source code with the DbSet property added</returns>
    public static string AddDbSetProperty(string sourceCode, string entityName, string? entityNamespace = null)
    {
        var tree = CSharpSyntaxTree.ParseText(sourceCode);
        var root = (CompilationUnitSyntax)tree.GetRoot();

        // Find the DbContext class
        var dbContextClass = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(c => InheritsFromDbContext(c));

        if (dbContextClass == null)
        {
            throw new InvalidOperationException("No DbContext class found in the source file.");
        }

        // Check if DbSet property already exists
        if (HasDbSetProperty(dbContextClass, entityName))
        {
            throw new InvalidOperationException($"DbSet<{entityName}> property already exists in the DbContext.");
        }

        // Create the DbSet property
        var dbSetProperty = CreateDbSetProperty(entityName);

        // Add the property to the class
        var updatedClass = dbContextClass.AddMembers(dbSetProperty);

        // Replace the old class with the updated class
        var newRoot = root.ReplaceNode(dbContextClass, updatedClass);

        // Add using statement if namespace provided
        if (!string.IsNullOrEmpty(entityNamespace))
        {
            newRoot = AddUsingDirective(newRoot, entityNamespace);
        }

        // Format the code
        var workspace = new AdhocWorkspace();
        var formattedRoot = Formatter.Format(newRoot, workspace);

        return formattedRoot.ToFullString();
    }

    /// <summary>
    /// Checks if a class inherits from DbContext.
    /// </summary>
    private static bool InheritsFromDbContext(ClassDeclarationSyntax classDeclaration)
    {
        if (classDeclaration.BaseList == null)
            return false;

        return classDeclaration.BaseList.Types
            .Any(t => t.Type.ToString().Contains("DbContext"));
    }

    /// <summary>
    /// Checks if a DbSet property for the entity already exists.
    /// </summary>
    private static bool HasDbSetProperty(ClassDeclarationSyntax classDeclaration, string entityName)
    {
        return classDeclaration.Members
            .OfType<PropertyDeclarationSyntax>()
            .Any(p => p.Type.ToString().Contains($"DbSet<{entityName}>"));
    }

    /// <summary>
    /// Creates a DbSet property syntax node.
    /// </summary>
    private static PropertyDeclarationSyntax CreateDbSetProperty(string entityName)
    {
        var propertyName = GetPluralName(entityName);

        // Create: public DbSet<EntityName> EntityNames => Set<EntityName>();
        var property = SyntaxFactory.PropertyDeclaration(
                SyntaxFactory.GenericName(
                    SyntaxFactory.Identifier("DbSet"))
                .WithTypeArgumentList(
                    SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                            SyntaxFactory.IdentifierName(entityName)))),
                SyntaxFactory.Identifier(propertyName))
            .WithModifiers(
                SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithExpressionBody(
                SyntaxFactory.ArrowExpressionClause(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.GenericName(
                            SyntaxFactory.Identifier("Set"))
                        .WithTypeArgumentList(
                            SyntaxFactory.TypeArgumentList(
                                SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                    SyntaxFactory.IdentifierName(entityName)))))))
            .WithSemicolonToken(
                SyntaxFactory.Token(SyntaxKind.SemicolonToken))
            .WithLeadingTrivia(
                SyntaxFactory.CarriageReturnLineFeed,
                SyntaxFactory.Whitespace("    "))
            .WithTrailingTrivia(
                SyntaxFactory.CarriageReturnLineFeed);

        return property;
    }

    /// <summary>
    /// Adds a using directive to the compilation unit if it doesn't exist.
    /// </summary>
    private static CompilationUnitSyntax AddUsingDirective(CompilationUnitSyntax root, string namespaceName)
    {
        // Check if using directive already exists (exact match)
        var existingUsing = root.Usings.FirstOrDefault(u => u.Name?.ToString() == namespaceName);
        if (existingUsing != null)
            return root;

        // Check if a qualified version already exists (e.g., "MyApp.Models" when adding "Models")
        // Or vice versa (e.g., "Models" exists when adding "MyApp.Models")
        var namespaceToAdd = namespaceName;
        var existingConflict = root.Usings.FirstOrDefault(u =>
        {
            var existing = u.Name?.ToString();
            if (existing == null) return false;
            
            // Check if one is a suffix of the other (e.g., "Models" vs "MyApp.Models")
            return existing.EndsWith("." + namespaceToAdd) || namespaceToAdd.EndsWith("." + existing);
        });

        // If a more specific (qualified) version exists, don't add the simple one
        if (existingConflict != null)
        {
            var existing = existingConflict.Name?.ToString();
            // Keep the more qualified version (longer namespace)
            if (existing != null && existing.Length > namespaceToAdd.Length)
            {
                return root; // Don't add the shorter version
            }
            
            // Remove the shorter version and add the longer one
            root = root.RemoveNode(existingConflict, SyntaxRemoveOptions.KeepNoTrivia) ?? root;
        }

        // Create new using directive
        var usingDirective = SyntaxFactory.UsingDirective(
            SyntaxFactory.ParseName(namespaceName))
            .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

        // Add using directive (maintaining alphabetical order)
        var usings = root.Usings.ToList();
        usings.Add(usingDirective);
        usings = usings.OrderBy(u => u.Name?.ToString()).ToList();

        return root.WithUsings(SyntaxFactory.List(usings));
    }

    /// <summary>
    /// Converts entity name to plural form for DbSet property name.
    /// Simple implementation - can be enhanced with better pluralization rules.
    /// </summary>
    private static string GetPluralName(string entityName)
    {
        // Simple pluralization rules
        if (entityName.EndsWith("s", StringComparison.OrdinalIgnoreCase) ||
            entityName.EndsWith("x", StringComparison.OrdinalIgnoreCase) ||
            entityName.EndsWith("ch", StringComparison.OrdinalIgnoreCase) ||
            entityName.EndsWith("sh", StringComparison.OrdinalIgnoreCase))
        {
            return entityName + "es";
        }
        
        if (entityName.EndsWith("y", StringComparison.OrdinalIgnoreCase))
        {
            return entityName.Substring(0, entityName.Length - 1) + "ies";
        }

        return entityName + "s";
    }

    /// <summary>
    /// Finds the DbContext file in the project directory.
    /// </summary>
    /// <param name="projectDirectory">The project directory to search</param>
    /// <returns>Path to the DbContext file, or null if not found</returns>
    public static string? FindDbContextFile(string projectDirectory)
    {
        // Common locations for DbContext
        var searchPatterns = new[]
        {
            "Data/*DbContext.cs",
            "*DbContext.cs",
            "Persistence/*DbContext.cs",
            "Infrastructure/*DbContext.cs"
        };

        foreach (var pattern in searchPatterns)
        {
            try
            {
                var files = Directory.GetFiles(projectDirectory, pattern, SearchOption.AllDirectories);
                if (files.Length > 0)
                {
                    // If multiple found, prefer the one with "DbContext" in the name
                    var dbContextFile = files.FirstOrDefault(f => Path.GetFileName(f).Contains("DbContext"));
                    return dbContextFile ?? files[0];
                }
            }
            catch (DirectoryNotFoundException)
            {
                // Directory doesn't exist, continue to next pattern
                continue;
            }
        }

        return null;
    }

    /// <summary>
    /// Validates that the source code can be parsed.
    /// </summary>
    public static bool IsValidCSharpCode(string sourceCode)
    {
        try
        {
            var tree = CSharpSyntaxTree.ParseText(sourceCode);
            var diagnostics = tree.GetDiagnostics();
            return !diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Extracts the namespace from a C# file.
    /// </summary>
    public static string? ExtractNamespace(string sourceCode)
    {
        var tree = CSharpSyntaxTree.ParseText(sourceCode);
        var root = (CompilationUnitSyntax)tree.GetRoot();

        // Try file-scoped namespace first (C# 10+)
        var fileScopedNamespace = root.DescendantNodes()
            .OfType<FileScopedNamespaceDeclarationSyntax>()
            .FirstOrDefault();

        if (fileScopedNamespace != null)
        {
            return fileScopedNamespace.Name.ToString();
        }

        // Fall back to block-scoped namespace
        var blockNamespace = root.DescendantNodes()
            .OfType<NamespaceDeclarationSyntax>()
            .FirstOrDefault();

        return blockNamespace?.Name.ToString();
    }

    /// <summary>
    /// Extracts all class names from a C# file.
    /// </summary>
    public static IEnumerable<string> ExtractClassNames(string sourceCode)
    {
        var tree = CSharpSyntaxTree.ParseText(sourceCode);
        var root = (CompilationUnitSyntax)tree.GetRoot();

        return root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Select(c => c.Identifier.Text);
    }
}
