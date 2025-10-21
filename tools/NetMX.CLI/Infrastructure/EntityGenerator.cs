using System.Text;
using NetMX.CLI.Models;

namespace NetMX.CLI.Infrastructure;

/// <summary>
/// Generates C# entity code from EntityGenerationOptions.
/// Produces DDD-compliant entities with validation, Guard clauses, and navigation properties.
/// </summary>
public class EntityGenerator
{
    /// <summary>
    /// Generates complete entity class code.
    /// </summary>
    public static string GenerateEntity(EntityGenerationOptions options)
    {
        var sb = new StringBuilder();

        // Usings
        sb.AppendLine("using System.ComponentModel.DataAnnotations;");
        sb.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
        sb.AppendLine("using NetMX.Ddd.Domain.Entities;");
        
        if (options.Properties.Any(p => p.IsRequired))
        {
            sb.AppendLine("using NetMX.Core; // For Guard clauses");
        }

        sb.AppendLine();

        // Namespace
        var namespaceName = options.ModuleName != null 
            ? $"{options.ModuleName}.Core.Entities"
            : options.ProjectNamespace != null
                ? $"{options.ProjectNamespace}.Models"
                : "Models";
        
        sb.AppendLine($"namespace {namespaceName};");
        sb.AppendLine();

        // Class XML comment
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// {options.EntityName} entity");
        sb.AppendLine("/// </summary>");

        // Class declaration
        sb.AppendLine($"public class {options.EntityName} : {options.BaseClass}");
        sb.AppendLine("{");

        // Properties
        foreach (var prop in options.Properties)
        {
            var propCode = PropertyParser.GeneratePropertyDeclaration(prop);
            sb.AppendLine(propCode);
            sb.AppendLine();
        }

        // Audit fields (if enabled)
        if (options.IncludeAuditFields)
        {
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Created date (UTC)");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;");
            sb.AppendLine();

            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Last updated date (UTC)");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public DateTime? UpdatedAt { get; private set; }");
            sb.AppendLine();
        }

        // Soft delete fields (if enabled)
        if (options.IncludeSoftDelete)
        {
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Soft delete flag");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public bool IsDeleted { get; private set; }");
            sb.AppendLine();

            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Deleted date (UTC)");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public DateTime? DeletedAt { get; private set; }");
            sb.AppendLine();
        }

        // Navigation properties (for foreign keys)
        var navigationProps = options.Properties.Where(p => p.IsNavigationProperty && !p.IsCollection).ToList();
        foreach (var prop in navigationProps)
        {
            if (prop.ForeignKey != null)
            {
                sb.AppendLine("    /// <summary>");
                sb.AppendLine($"    /// Navigation property to {prop.ForeignKey}");
                sb.AppendLine("    /// </summary>");
                sb.AppendLine($"    public {prop.ForeignKey}? {prop.ForeignKey} {{ get; set; }}");
                sb.AppendLine();
            }
        }

        // Collection navigation properties (for many-to-many)
        var collectionProps = options.Properties.Where(p => p.IsCollection && p.IsNavigationProperty).ToList();
        foreach (var prop in collectionProps)
        {
            if (prop.ForeignKey != null)
            {
                sb.AppendLine("    /// <summary>");
                sb.AppendLine($"    /// Navigation collection to {prop.ForeignKey}");
                sb.AppendLine("    /// </summary>");
                sb.AppendLine($"    public ICollection<{prop.ForeignKey}> {prop.ForeignKey}s {{ get; set; }} = new List<{prop.ForeignKey}>();");
                sb.AppendLine();
            }
        }

        // Private constructor (for EF Core)
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Private constructor for EF Core");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine($"    private {options.EntityName}() {{ }}");
        sb.AppendLine();

        // Public constructor
        GenerateConstructor(sb, options);

        // Update methods
        GenerateUpdateMethods(sb, options);

        // Soft delete methods (if enabled)
        if (options.IncludeSoftDelete)
        {
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Soft delete this entity");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public void Delete()");
            sb.AppendLine("    {");
            sb.AppendLine("        IsDeleted = true;");
            sb.AppendLine("        DeletedAt = DateTime.UtcNow;");
            sb.AppendLine("    }");
            sb.AppendLine();

            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Restore this entity");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public void Restore()");
            sb.AppendLine("    {");
            sb.AppendLine("        IsDeleted = false;");
            sb.AppendLine("        DeletedAt = null;");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        // Close class
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Generates public constructor with all required properties.
    /// </summary>
    private static void GenerateConstructor(StringBuilder sb, EntityGenerationOptions options)
    {
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Creates a new {options.EntityName}");
        sb.AppendLine("    /// </summary>");

        // Constructor parameters (required properties only)
        var requiredProps = options.Properties.Where(p => p.IsRequired).ToList();
        var parameters = new List<string> { $"{options.KeyType} id" };
        parameters.AddRange(requiredProps.Select(PropertyParser.GenerateConstructorParameter));

        sb.Append($"    public {options.EntityName}(");
        sb.Append(string.Join(", ", parameters));
        sb.AppendLine(")");
        sb.AppendLine("    {");
        
        // Set Id property
        sb.AppendLine("        Id = id;");

        // Constructor assignments
        foreach (var prop in requiredProps)
        {
            sb.AppendLine(PropertyParser.GenerateConstructorAssignment(prop));
        }

        sb.AppendLine("    }");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates update methods for mutable properties.
    /// </summary>
    private static void GenerateUpdateMethods(StringBuilder sb, EntityGenerationOptions options)
    {
        // Generate Set methods for each property
        foreach (var prop in options.Properties)
        {
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// Update {prop.Name}");
            sb.AppendLine("    /// </summary>");

            var nullable = prop.IsNullable ? "?" : "";
            var paramName = char.ToLower(prop.Name[0]) + prop.Name[1..];

            sb.AppendLine($"    public void Set{prop.Name}({prop.Type}{nullable} {paramName})");
            sb.AppendLine("    {");

            // Validation (for required properties)
            if (prop.IsRequired && prop.Type == "string")
            {
                sb.AppendLine($"        {prop.Name} = Guard.NotNullOrEmpty({paramName}, nameof({paramName}));");
            }
            else if (prop.IsRequired)
            {
                sb.AppendLine($"        {prop.Name} = Guard.NotNull({paramName}, nameof({paramName}));");
            }
            else
            {
                sb.AppendLine($"        {prop.Name} = {paramName};");
            }

            // Update timestamp
            if (options.IncludeAuditFields)
            {
                sb.AppendLine("        UpdatedAt = DateTime.UtcNow;");
            }

            sb.AppendLine("    }");
            sb.AppendLine();
        }
    }

    /// <summary>
    /// Generates enum class for enum properties.
    /// </summary>
    public static string GenerateEnum(PropertyDefinition prop)
    {
        if (!prop.IsEnum || prop.EnumValues == null || prop.EnumValues.Count == 0)
        {
            throw new ArgumentException("Property must be an enum with values");
        }

        var sb = new StringBuilder();

        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// {prop.Type} enumeration");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public enum {prop.Type}");
        sb.AppendLine("{");

        for (int i = 0; i < prop.EnumValues.Count; i++)
        {
            var value = prop.EnumValues[i];
            sb.Append($"    {value} = {i}");
            if (i < prop.EnumValues.Count - 1)
            {
                sb.AppendLine(",");
            }
            else
            {
                sb.AppendLine();
            }
        }

        sb.AppendLine("}");

        return sb.ToString();
    }
}
