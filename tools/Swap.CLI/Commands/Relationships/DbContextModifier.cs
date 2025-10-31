using Swap.CLI.Commands.Relationships.Models;

namespace Swap.CLI.Commands.Relationships;

/// <summary>
/// Modifies DbContext to configure entity relationships
/// Uses simple string manipulation for reliability
/// </summary>
public class DbContextModifier
{
    /// <summary>
    /// Add relationship configuration to DbContext OnModelCreating method
    /// </summary>
    public static async Task<string> ConfigureRelationshipAsync(
        string dbContextPath,
        RelationshipDefinition definition)
    {
        var code = await File.ReadAllTextAsync(dbContextPath);
        var configCode = GenerateConfigurationCode(definition);

        // Find OnModelCreating method
        var onModelCreatingIndex = code.IndexOf("protected override void OnModelCreating", StringComparison.Ordinal);
        
        if (onModelCreatingIndex == -1)
        {
            // Method doesn't exist - add it before the closing brace of the class
            var lastBraceIndex = code.LastIndexOf('}');
            if (lastBraceIndex == -1)
            {
                throw new InvalidOperationException("Could not find class closing brace in DbContext");
            }

            var newMethod = $@"
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {{
        base.OnModelCreating(modelBuilder);

{configCode}
    }}
}}";
            return code.Substring(0, lastBraceIndex) + newMethod;
        }
        else
        {
            // Method exists - find a safe insertion point
            // Strategy: Look for "base.OnModelCreating" or the opening brace of the method,
            // then insert after the first statement (or at start if no base call)
            
            var methodStart = onModelCreatingIndex;
            
            // Find the opening brace of the method
            var openBraceIndex = code.IndexOf('{', methodStart);
            if (openBraceIndex == -1)
            {
                throw new InvalidOperationException("Could not find OnModelCreating opening brace");
            }
            
            // Look for base.OnModelCreating call
            var baseCallIndex = code.IndexOf("base.OnModelCreating", methodStart);
            
            int insertPosition;
            if (baseCallIndex != -1 && baseCallIndex < code.Length)
            {
                // Find the end of the base call (semicolon)
                var semicolonIndex = code.IndexOf(';', baseCallIndex);
                if (semicolonIndex != -1)
                {
                    // Insert after the semicolon and newline
                    var nextNewline = code.IndexOf('\n', semicolonIndex);
                    insertPosition = nextNewline != -1 ? nextNewline + 1 : semicolonIndex + 1;
                }
                else
                {
                    insertPosition = openBraceIndex + 1;
                }
            }
            else
            {
                // No base call, insert right after opening brace
                var nextNewline = code.IndexOf('\n', openBraceIndex);
                insertPosition = nextNewline != -1 ? nextNewline + 1 : openBraceIndex + 1;
            }

            // Insert configuration with proper indentation
            return code.Substring(0, insertPosition) + $"\n{configCode}\n" + code.Substring(insertPosition);
        }
    }

    private static string GenerateConfigurationCode(RelationshipDefinition definition)
    {
        if (definition.Type == RelationshipType.OneToMany)
        {
            return GenerateOneToManyConfig(definition);
        }
        else if (definition.Type == RelationshipType.ManyToOne)
        {
            return GenerateManyToOneConfig(definition);
        }

        return string.Empty;
    }

    private static string GenerateOneToManyConfig(RelationshipDefinition definition)
    {
        // For OneToMany (Category->Product): Target (Product) has FK to Source (Category)
        var fkName = definition.ForeignKeyName ?? $"{definition.SourceEntity}Id";
        var navProp = definition.NavigationProperty ?? definition.SourceEntity;  // Product.Category
        var inverseProp = definition.InverseNavigation ?? EntityModifier.Pluralize(definition.TargetEntity);  // Category.Products
        
        var deleteAction = definition.OnDelete switch
        {
            DeleteBehavior.Cascade => "DeleteBehavior.Cascade",
            DeleteBehavior.SetNull => "DeleteBehavior.SetNull",
            _ => "DeleteBehavior.Restrict"
        };

        var required = definition.IsRequired ? "IsRequired()" : "IsRequired(false)";

        return $@"        // {definition.SourceEntity} -> {definition.TargetEntity} (One-to-Many)
        modelBuilder.Entity<{definition.TargetEntity}>()
            .HasOne(e => e.{navProp})
            .WithMany(e => e.{inverseProp})
            .HasForeignKey(e => e.{fkName})
            .{required}
            .OnDelete({deleteAction});";
    }

    private static string GenerateManyToOneConfig(RelationshipDefinition definition)
    {
        // For many-to-one: source is "many" side, target is "one" side
        // Example: Order->Customer, many Orders to one Customer
        // Order (source) has FK CustomerId and navigation to Customer
        // Customer (target) has collection of Orders
        
        var fkName = definition.ForeignKeyName ?? $"{definition.TargetEntity}Id";
        var navProp = definition.NavigationProperty ?? definition.TargetEntity;
        var inverseProp = definition.InverseNavigation ?? EntityModifier.Pluralize(definition.SourceEntity);
        
        var deleteAction = definition.OnDelete switch
        {
            DeleteBehavior.Cascade => "DeleteBehavior.Cascade",
            DeleteBehavior.SetNull => "DeleteBehavior.SetNull",
            _ => "DeleteBehavior.Restrict"
        };

        var required = definition.IsRequired ? "IsRequired()" : "IsRequired(false)";

        return $@"        // {definition.SourceEntity} -> {definition.TargetEntity} (Many-to-One)
        modelBuilder.Entity<{definition.SourceEntity}>()
            .HasOne(e => e.{navProp})
            .WithMany(e => e.{inverseProp})
            .HasForeignKey(e => e.{fkName})
            .{required}
            .OnDelete({deleteAction});";
    }

    /// <summary>
    /// Find the DbContext file in the project
    /// </summary>
    public static string? FindDbContextFile(string projectPath)
    {
        var dataDir = Path.Combine(projectPath, "Data");
        if (!Directory.Exists(dataDir))
        {
            return null;
        }

        var dbContextFiles = Directory.GetFiles(dataDir, "*DbContext.cs", SearchOption.TopDirectoryOnly);
        return dbContextFiles.FirstOrDefault();
    }
}
