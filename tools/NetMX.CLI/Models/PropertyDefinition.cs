namespace NetMX.CLI.Models;

/// <summary>
/// Represents a parsed property definition from CLI input.
/// Example: "name:string:256:required" or "price:decimal:18:2:min:0"
/// </summary>
public class PropertyDefinition
{
    /// <summary>
    /// Property name (e.g., "Name", "Price")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// C# type name (e.g., "string", "int", "decimal", "Guid")
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Original CLI type (e.g., "string", "text", "guid", "bool")
    /// </summary>
    public string CliType { get; set; } = string.Empty;

    /// <summary>
    /// Is this property required? (adds [Required])
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Is this property nullable? (adds ?)
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// Max length for strings (adds [MaxLength(n)])
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// Precision for decimals (e.g., 18 in decimal:18:2)
    /// </summary>
    public int? Precision { get; set; }

    /// <summary>
    /// Scale for decimals (e.g., 2 in decimal:18:2)
    /// </summary>
    public int? Scale { get; set; }

    /// <summary>
    /// Minimum value (adds [Range(min, max)])
    /// </summary>
    public string? MinValue { get; set; }

    /// <summary>
    /// Maximum value (adds [Range(min, max)])
    /// </summary>
    public string? MaxValue { get; set; }

    /// <summary>
    /// Default value (e.g., "true", "Draft", "0")
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Foreign key reference (e.g., "Category" in "categoryId:guid:fk:Category")
    /// </summary>
    public string? ForeignKey { get; set; }

    /// <summary>
    /// Display property for foreign key (e.g., "Name" in "categoryId:guid:fk:Category:Name")
    /// </summary>
    public string? ForeignKeyDisplay { get; set; }

    /// <summary>
    /// Is this an enum type?
    /// </summary>
    public bool IsEnum { get; set; }

    /// <summary>
    /// Enum values (e.g., ["Draft", "Published", "Archived"])
    /// </summary>
    public List<string> EnumValues { get; set; } = new();

    /// <summary>
    /// Is this a collection? (e.g., guid[] for many-to-many)
    /// </summary>
    public bool IsCollection { get; set; }

    /// <summary>
    /// Is this a navigation property?
    /// </summary>
    public bool IsNavigationProperty { get; set; }

    /// <summary>
    /// Raw CLI input (for debugging)
    /// </summary>
    public string RawInput { get; set; } = string.Empty;
}
