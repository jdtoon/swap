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
    /// Add DbSet property for a junction table entity
    /// </summary>
    public static async Task<string> AddDbSetAsync(string dbContextPath, string junctionTableName)
    {
        var code = await File.ReadAllTextAsync(dbContextPath);
        
        // Check if DbSet already exists
        var modelsPrefix = code.Contains($"DbSet<Models.") ? "Models." : "";
        
        // Check if junction table DbSet already exists
        if (code.Contains($"DbSet<{modelsPrefix}{junctionTableName}>"))
        {
            return code;
        }

        // Find the class declaration
        var classIndex = code.IndexOf("class", StringComparison.Ordinal);
        if (classIndex == -1)
        {
            throw new InvalidOperationException("Could not find class declaration in DbContext");
        }

        // Find the opening brace of the class
        var openBraceIndex = code.IndexOf('{', classIndex);
        if (openBraceIndex == -1)
        {
            throw new InvalidOperationException("Could not find opening brace of DbContext class");
        }

        // Find existing DbSet properties to maintain consistent formatting
        var lastDbSetIndex = code.LastIndexOf("DbSet<", openBraceIndex + 1);
        
        string dbSetProperty = $"    public DbSet<{modelsPrefix}{junctionTableName}> {junctionTableName} {{ get; set; }}";
        
        if (lastDbSetIndex > openBraceIndex)
        {
            // Insert after the last DbSet
            var endOfLineIndex = code.IndexOf('\n', lastDbSetIndex);
            if (endOfLineIndex == -1)
            {
                endOfLineIndex = code.Length;
            }
            
            return code.Insert(endOfLineIndex + 1, dbSetProperty + "\n");
        }
        else
        {
            // No existing DbSets - insert after class opening brace
            return code.Insert(openBraceIndex + 1, "\n" + dbSetProperty + "\n");
        }
    }

    /// <summary>
    /// Configure many-to-many relationship in DbContext
    /// </summary>
    public static async Task<string> ConfigureManyToManyAsync(
        string dbContextCode,
        RelationshipDefinition definition,
        string junctionTableName)
    {
        var code = dbContextCode;
        
        // Detect if we need Models. prefix
        var modelsPrefix = code.Contains("DbSet<Models.") ? "Models." : "";
        
        // Generate configuration code for many-to-many
        var sourceEntity = definition.SourceEntity;
        var targetEntity = definition.TargetEntity;
        var sourceNavProp = definition.NavigationProperty ?? EntityModifier.Pluralize(targetEntity);
        var targetNavProp = definition.InverseNavigation ?? EntityModifier.Pluralize(sourceEntity);

        var configCode = $@"        // Many-to-many: {sourceEntity} ↔ {targetEntity}
        modelBuilder.Entity<{modelsPrefix}{sourceEntity}>()
            .HasMany(e => e.{sourceNavProp})
            .WithMany(e => e.{targetNavProp})
            .UsingEntity<{modelsPrefix}{junctionTableName}>(
                j => j
                    .HasOne<{modelsPrefix}{targetEntity}>(x => x.{targetEntity})
                    .WithMany()
                    .HasForeignKey(x => x.{targetEntity}Id),
                j => j
                    .HasOne<{modelsPrefix}{sourceEntity}>(x => x.{sourceEntity})
                    .WithMany()
                    .HasForeignKey(x => x.{sourceEntity}Id),
                j =>
                {{
                    j.HasKey(x => new {{ x.{sourceEntity}Id, x.{targetEntity}Id }});
                }});
";

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
            // Method exists - find insertion point
            var methodStart = onModelCreatingIndex;
            var openBraceIndex = code.IndexOf('{', methodStart);
            
            if (openBraceIndex == -1)
            {
                throw new InvalidOperationException("Could not find opening brace of OnModelCreating method");
            }

            // Look for base.OnModelCreating call
            var baseCallIndex = code.IndexOf("base.OnModelCreating", openBraceIndex);
            int insertionPoint;

            if (baseCallIndex > openBraceIndex && baseCallIndex < code.IndexOf('}', openBraceIndex))
            {
                // Insert after the base call
                var semicolonIndex = code.IndexOf(';', baseCallIndex);
                if (semicolonIndex == -1)
                {
                    throw new InvalidOperationException("Could not find semicolon after base.OnModelCreating call");
                }
                insertionPoint = code.IndexOf('\n', semicolonIndex) + 1;
            }
            else
            {
                // No base call - insert at the start of the method body
                insertionPoint = code.IndexOf('\n', openBraceIndex) + 1;
            }

            return code.Insert(insertionPoint, "\n" + configCode);
        }
    }

    /// <summary>
    /// Configure one-to-one relationship in DbContext
    /// </summary>
    public static async Task<string> ConfigureOneToOneAsync(
        string dbContextPath,
        RelationshipDefinition definition,
        string principalEntity,
        string dependentEntity)
    {
        var code = await File.ReadAllTextAsync(dbContextPath);
        var configCode = GenerateOneToOneConfig(definition, principalEntity, dependentEntity);

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
            // Method exists - find insertion point
            var methodStart = onModelCreatingIndex;
            var openBraceIndex = code.IndexOf('{', methodStart);
            
            if (openBraceIndex == -1)
            {
                throw new InvalidOperationException("Could not find opening brace of OnModelCreating method");
            }

            // Look for base.OnModelCreating call
            var baseCallIndex = code.IndexOf("base.OnModelCreating", openBraceIndex);
            int insertionPoint;

            if (baseCallIndex > openBraceIndex && baseCallIndex < code.IndexOf('}', openBraceIndex))
            {
                // Insert after the base call
                var semicolonIndex = code.IndexOf(';', baseCallIndex);
                if (semicolonIndex == -1)
                {
                    throw new InvalidOperationException("Could not find semicolon after base.OnModelCreating call");
                }
                insertionPoint = code.IndexOf('\n', semicolonIndex) + 1;
            }
            else
            {
                // No base call - insert at the start of the method body
                insertionPoint = code.IndexOf('\n', openBraceIndex) + 1;
            }

            return code.Insert(insertionPoint, "\n" + configCode);
        }
    }

    private static string GenerateOneToOneConfig(
        RelationshipDefinition definition,
        string principalEntity,
        string dependentEntity)
    {
        var fkName = definition.ForeignKeyName ?? $"{principalEntity}Id";
        var principalNavProp = definition.NavigationProperty ?? dependentEntity;
        var dependentNavProp = definition.InverseNavigation ?? principalEntity;
        
        var deleteAction = definition.OnDelete switch
        {
            DeleteBehavior.Cascade => "DeleteBehavior.Cascade",
            DeleteBehavior.SetNull => "DeleteBehavior.SetNull",
            _ => "DeleteBehavior.Restrict"
        };

        var required = definition.IsRequired ? "IsRequired()" : "IsRequired(false)";

        return $@"        // {principalEntity} <-> {dependentEntity} (One-to-One)
        modelBuilder.Entity<{principalEntity}>()
            .HasOne(e => e.{principalNavProp})
            .WithOne(e => e.{dependentNavProp})
            .HasForeignKey<{dependentEntity}>(e => e.{fkName})
            .{required}
            .OnDelete({deleteAction});
        
        // Ensure unique constraint on FK
        modelBuilder.Entity<{dependentEntity}>()
            .HasIndex(e => e.{fkName})
            .IsUnique();";
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
