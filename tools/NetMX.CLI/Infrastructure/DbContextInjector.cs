using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetMX.CLI.Infrastructure;

/// <summary>
/// Uses Roslyn to inject DbSet properties into DbContext files.
/// Zero manual editing required!
/// </summary>
public class DbContextInjector
{
    /// <summary>
    /// Adds a DbSet property to the DbContext class.
    /// </summary>
    /// <param name="dbContextPath">Path to the DbContext.cs file</param>
    /// <param name="entityName">Name of the entity (e.g., "Product")</param>
    /// <returns>True if successful, false otherwise</returns>
    public static async Task<bool> AddDbSetAsync(string dbContextPath, string entityName)
    {
        if (!File.Exists(dbContextPath))
        {
            Console.WriteLine($"❌ DbContext file not found: {dbContextPath}");
            return false;
        }

        try
        {
            // Read existing file
            var code = await File.ReadAllTextAsync(dbContextPath);
            
            // Parse with Roslyn
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = await tree.GetRootAsync();

            // Find the DbContext class
            var classDeclaration = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault(c => c.Identifier.Text.EndsWith("DbContext"));

            if (classDeclaration == null)
            {
                Console.WriteLine($"❌ No DbContext class found in {dbContextPath}");
                return false;
            }

            // Check if DbSet already exists
            var existingDbSet = classDeclaration.Members
                .OfType<PropertyDeclarationSyntax>()
                .Any(p => p.Identifier.Text == $"{entityName}s");

            if (existingDbSet)
            {
                Console.WriteLine($"ℹ️  DbSet<{entityName}> already exists in DbContext");
                return true; // Not an error, just already exists
            }

            // Create DbSet property syntax
            var dbSetProperty = SyntaxFactory.PropertyDeclaration(
                SyntaxFactory.GenericName(
                    SyntaxFactory.Identifier("DbSet"),
                    SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                            SyntaxFactory.IdentifierName(entityName)
                        )
                    )
                ),
                SyntaxFactory.Identifier($"{entityName}s")
            )
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .WithExpressionBody(
                SyntaxFactory.ArrowExpressionClause(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.GenericName(
                            SyntaxFactory.Identifier("Set"),
                            SyntaxFactory.TypeArgumentList(
                                SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                    SyntaxFactory.IdentifierName(entityName)
                                )
                            )
                        )
                    )
                )
            )
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
            .NormalizeWhitespace();

            // Add XML documentation comment
            var xmlComment = SyntaxFactory.TriviaList(
                SyntaxFactory.Trivia(
                    SyntaxFactory.DocumentationCommentTrivia(
                        SyntaxKind.SingleLineDocumentationCommentTrivia,
                        SyntaxFactory.List(new XmlNodeSyntax[]
                        {
                            SyntaxFactory.XmlText("/// "),
                            SyntaxFactory.XmlElement(
                                SyntaxFactory.XmlElementStartTag(SyntaxFactory.XmlName("summary")),
                                SyntaxFactory.SingletonList<XmlNodeSyntax>(
                                    SyntaxFactory.XmlText($"Gets the DbSet for {entityName} entities.")
                                ),
                                SyntaxFactory.XmlElementEndTag(SyntaxFactory.XmlName("summary"))
                            ),
                            SyntaxFactory.XmlText(Environment.NewLine + "    ")
                        })
                    )
                )
            );

            dbSetProperty = dbSetProperty.WithLeadingTrivia(xmlComment);

            // Insert the property before the last member (or at the end if empty)
            var newClass = classDeclaration.AddMembers(dbSetProperty);
            var newRoot = root.ReplaceNode(classDeclaration, newClass);

            // Write back to file
            var newCode = newRoot.ToFullString();
            await File.WriteAllTextAsync(dbContextPath, newCode);

            Console.WriteLine($"✅ Added DbSet<{entityName}> to {Path.GetFileName(dbContextPath)}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to inject DbSet: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Finds the DbContext file in the current solution.
    /// </summary>
    public static string? FindDbContext()
    {
        var currentDir = Directory.GetCurrentDirectory();
        
        // Search patterns
        var patterns = new[]
        {
            "*DbContext.cs",
            "AppDbContext.cs",
            "ApplicationDbContext.cs"
        };

        foreach (var pattern in patterns)
        {
            var files = Directory.GetFiles(currentDir, pattern, SearchOption.AllDirectories);
            
            // Filter out bin/obj directories
            var validFiles = files.Where(f => 
                !f.Contains("\\bin\\") && 
                !f.Contains("\\obj\\") &&
                !f.Contains("/bin/") &&
                !f.Contains("/obj/")
            ).ToList();

            if (validFiles.Any())
            {
                return validFiles.First();
            }
        }

        return null;
    }
}
