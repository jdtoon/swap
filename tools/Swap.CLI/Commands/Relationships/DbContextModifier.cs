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
            // Method exists - find the closing brace and insert before it
            var methodStart = onModelCreatingIndex;
            var braceCount = 0;
            var foundFirstBrace = false;
            var insertPosition = -1;

            for (int i = methodStart; i < code.Length; i++)
            {
                if (code[i] == '{')
                {
                    foundFirstBrace = true;
                    braceCount++;
                }
                else if (code[i] == '}')
                {
                    braceCount--;
                    if (foundFirstBrace && braceCount == 0)
                    {
                        // Found the closing brace of the method
                        insertPosition = i;
                        break;
                    }
                }
            }

            if (insertPosition == -1)
            {
                throw new InvalidOperationException("Could not find OnModelCreating method closing brace");
            }

            // Insert configuration before the closing brace
            var configWithNewline = $"\n{configCode}\n    ";
            return code.Substring(0, insertPosition) + configWithNewline + code.Substring(insertPosition);
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

        return $@"        // {definition.SourceEntity} -> {definition.TargetEntity} (One-to-Many)
        modelBuilder.Entity<{definition.SourceEntity}>()
            .HasOne(e => e.{navProp})
            .WithMany(e => e.{inverseProp})
            .HasForeignKey(e => e.{fkName})
            .{required}
            .OnDelete({deleteAction});";
    }

    private static string GenerateManyToOneConfig(RelationshipDefinition definition)
    {
        // Many-to-one is just one-to-many from the other perspective
        var fkName = definition.ForeignKeyName ?? $"{definition.SourceEntity}Id";
        var navProp = definition.InverseNavigation ?? definition.SourceEntity;
        var inverseProp = definition.NavigationProperty ?? EntityModifier.Pluralize(definition.TargetEntity);
        
        var deleteAction = definition.OnDelete switch
        {
            DeleteBehavior.Cascade => "DeleteBehavior.Cascade",
            DeleteBehavior.SetNull => "DeleteBehavior.SetNull",
            _ => "DeleteBehavior.Restrict"
        };

        var required = definition.IsRequired ? "IsRequired()" : "IsRequired(false)";

        return $@"        // {definition.TargetEntity} -> {definition.SourceEntity} (Many-to-One)
        modelBuilder.Entity<{definition.TargetEntity}>()
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
