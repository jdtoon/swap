using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Swap.CLI.Commands.Relationships.Models;

namespace Swap.CLI.Commands.Relationships;

/// <summary>
/// Modifies entity classes to add relationship properties
/// </summary>
public class EntityModifier
{
    /// <summary>
    /// Add a foreign key and navigation property for a one-to-many or many-to-one relationship
    /// </summary>
    public static async Task<string> AddOneToManyPropertiesAsync(
        string entityFilePath,
        string entityName,
        RelationshipDefinition definition)
    {
        var code = await File.ReadAllTextAsync(entityFilePath);
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetCompilationUnitRoot();

        var classDeclaration = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(c => c.Identifier.Text == entityName);

        if (classDeclaration == null)
        {
            throw new InvalidOperationException($"Could not find class {entityName} in {entityFilePath}");
        }

        // Determine if this is the source or target entity
        bool isSource = string.Equals(entityName, definition.SourceEntity, StringComparison.OrdinalIgnoreCase);
        bool isTarget = string.Equals(entityName, definition.TargetEntity, StringComparison.OrdinalIgnoreCase);

        if (!isSource && !isTarget)
        {
            throw new InvalidOperationException($"Entity {entityName} is neither source nor target in relationship");
        }

        var newClass = classDeclaration;

        if (definition.Type == RelationshipType.OneToMany)
        {
            if (isSource)
            {
                // Source entity gets collection navigation (one-to-many side)
                // Example: Category has many Products -> Category.Products (collection)
                if (!definition.SkipNavigation)
                {
                    newClass = AddCollectionNavigation(newClass, definition.TargetEntity, definition.InverseNavigation);
                }
            }
            else // isTarget
            {
                // Target entity gets FK and navigation to source (many-to-one side)
                // Example: Product belongs to Category -> Product.CategoryId (FK) and Product.Category (nav)
                newClass = AddForeignKeyProperty(newClass, definition);
                if (!definition.SkipNavigation)
                {
                    newClass = AddNavigationProperty(newClass, definition.SourceEntity, definition.NavigationProperty);
                }
            }
        }
        else if (definition.Type == RelationshipType.ManyToOne)
        {
            // For many-to-one: source is "many" side, target is "one" side
            // Example: Order -> Customer (many-to-one) means many Orders to one Customer
            // Order (source) gets FK and navigation, Customer (target) gets collection
            
            if (isSource)
            {
                // Source entity is "many" side - gets FK and navigation to target
                // Example: Order gets CustomerId (FK) and Customer (navigation)
                newClass = AddForeignKeyProperty(newClass, definition);
                if (!definition.SkipNavigation)
                {
                    newClass = AddNavigationProperty(newClass, definition.TargetEntity, definition.NavigationProperty);
                }
            }
            else // isTarget
            {
                // Target entity is "one" side - gets collection navigation
                // Example: Customer gets ICollection<Order> Orders
                if (!definition.SkipNavigation)
                {
                    newClass = AddCollectionNavigation(newClass, definition.SourceEntity, definition.InverseNavigation);
                }
            }
        }

        var newRoot = root.ReplaceNode(classDeclaration, newClass);
        return newRoot.NormalizeWhitespace().ToFullString();
    }

    private static ClassDeclarationSyntax AddForeignKeyProperty(
        ClassDeclarationSyntax classDeclaration,
        RelationshipDefinition definition)
    {
        // Determine FK name and type based on relationship type
        string fkName;
        
        if (definition.Type == RelationshipType.OneToMany)
        {
            // For OneToMany: target (many side) gets FK to source (one side)
            // Example: Category->Product, Product gets CategoryId (SourceEntityId)
            fkName = definition.ForeignKeyName ?? $"{definition.SourceEntity}Id";
        }
        else if (definition.Type == RelationshipType.ManyToOne)
        {
            // For ManyToOne: source (many side) gets FK to target (one side)
            // Example: Order->Customer, Order gets CustomerId (TargetEntityId)
            fkName = definition.ForeignKeyName ?? $"{definition.TargetEntity}Id";
        }
        else
        {
            // Fallback for other types
            fkName = definition.ForeignKeyName ?? $"{definition.SourceEntity}Id";
        }
        
        var fkType = definition.IsRequired ? "int" : "int?";

        // Check if property already exists
        var existingProp = classDeclaration.Members
            .OfType<PropertyDeclarationSyntax>()
            .FirstOrDefault(p => p.Identifier.Text == fkName);

        if (existingProp != null)
        {
            return classDeclaration; // Already exists, skip
        }

        // Create FK property: public int? CategoryId { get; set; }
        var fkProperty = SyntaxFactory.PropertyDeclaration(
                SyntaxFactory.ParseTypeName(fkType),
                SyntaxFactory.Identifier(fkName))
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .AddAccessorListAccessors(
                SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));

        return classDeclaration.AddMembers(fkProperty);
    }

    private static ClassDeclarationSyntax AddNavigationProperty(
        ClassDeclarationSyntax classDeclaration,
        string targetEntity,
        string? customName)
    {
        var navName = customName ?? targetEntity;

        // Check if property already exists
        var existingProp = classDeclaration.Members
            .OfType<PropertyDeclarationSyntax>()
            .FirstOrDefault(p => p.Identifier.Text == navName);

        if (existingProp != null)
        {
            return classDeclaration; // Already exists, skip
        }

        // Create navigation property: public Customer? Customer { get; set; }
        var navProperty = SyntaxFactory.PropertyDeclaration(
                SyntaxFactory.ParseTypeName($"{targetEntity}?"),
                SyntaxFactory.Identifier(navName))
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .AddAccessorListAccessors(
                SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))
            .WithLeadingTrivia(SyntaxFactory.Whitespace("    "))
            .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

        return classDeclaration.AddMembers(navProperty);
    }

    private static ClassDeclarationSyntax AddCollectionNavigation(
        ClassDeclarationSyntax classDeclaration,
        string relatedEntity,
        string? customName)
    {
        // Default to plural form
        var navName = customName ?? Pluralize(relatedEntity);

        // Check if property already exists
        var existingProp = classDeclaration.Members
            .OfType<PropertyDeclarationSyntax>()
            .FirstOrDefault(p => p.Identifier.Text == navName);

        if (existingProp != null)
        {
            return classDeclaration; // Already exists, skip
        }

        // Create collection navigation: public ICollection<Order> Orders { get; set; } = new List<Order>();
        var collectionProperty = SyntaxFactory.PropertyDeclaration(
                SyntaxFactory.ParseTypeName($"ICollection<{relatedEntity}>"),
                SyntaxFactory.Identifier(navName))
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .AddAccessorListAccessors(
                SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))
            .WithInitializer(
                SyntaxFactory.EqualsValueClause(
                    SyntaxFactory.ObjectCreationExpression(
                        SyntaxFactory.ParseTypeName($"List<{relatedEntity}>"))
                    .WithArgumentList(SyntaxFactory.ArgumentList())))
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
            .WithLeadingTrivia(SyntaxFactory.Whitespace("    "))
            .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

        return classDeclaration.AddMembers(collectionProperty);
    }

    public static string Pluralize(string word)
    {
        // Simple pluralization rules
        if (word.EndsWith("y", StringComparison.OrdinalIgnoreCase) && word.Length > 1 && 
            !"aeiou".Contains(word[^2], StringComparison.OrdinalIgnoreCase))
        {
            return word.Substring(0, word.Length - 1) + "ies";
        }
        if (word.EndsWith("s", StringComparison.OrdinalIgnoreCase) ||
            word.EndsWith("x", StringComparison.OrdinalIgnoreCase) ||
            word.EndsWith("z", StringComparison.OrdinalIgnoreCase) ||
            word.EndsWith("ch", StringComparison.OrdinalIgnoreCase) ||
            word.EndsWith("sh", StringComparison.OrdinalIgnoreCase))
        {
            return word + "es";
        }
        return word + "s";
    }

    /// <summary>
    /// Check if an entity file exists
    /// </summary>
    public static bool EntityExists(string projectPath, string entityName)
    {
        var modelPath = Path.Combine(projectPath, "Models", $"{entityName}.cs");
        return File.Exists(modelPath);
    }

    /// <summary>
    /// Get the full path to an entity file
    /// </summary>
    public static string GetEntityPath(string projectPath, string entityName)
    {
        return Path.Combine(projectPath, "Models", $"{entityName}.cs");
    }
}
