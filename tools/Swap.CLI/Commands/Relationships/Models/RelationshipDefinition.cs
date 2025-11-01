namespace Swap.CLI.Commands.Relationships.Models;

/// <summary>
/// Represents a complete definition of a relationship between two entities
/// </summary>
public class RelationshipDefinition
{
    /// <summary>
    /// The source entity (e.g., "Order" in "Order → Customer")
    /// </summary>
    public string SourceEntity { get; set; } = string.Empty;

    /// <summary>
    /// The target entity (e.g., "Customer" in "Order → Customer")
    /// </summary>
    public string TargetEntity { get; set; } = string.Empty;

    /// <summary>
    /// Type of relationship (OneToMany, ManyToOne, ManyToMany, OneToOne)
    /// </summary>
    public RelationshipType Type { get; set; }

    /// <summary>
    /// Whether the foreign key is required (NOT NULL)
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// What happens when the parent entity is deleted
    /// </summary>
    public DeleteBehavior OnDelete { get; set; } = DeleteBehavior.Restrict;

    /// <summary>
    /// Field to display in dropdowns (e.g., "Name", "Title", "Email")
    /// If null, will be auto-detected
    /// </summary>
    public string? DisplayField { get; set; }

    /// <summary>
    /// Name of the foreign key property (e.g., "CustomerId")
    /// If null, will be auto-generated as {TargetEntity}Id
    /// </summary>
    public string? ForeignKeyName { get; set; }

    /// <summary>
    /// Name of the navigation property from source to target (e.g., "Customer")
    /// If null, will default to TargetEntity name
    /// </summary>
    public string? NavigationProperty { get; set; }

    /// <summary>
    /// Name of the inverse navigation property from target to source (e.g., "Orders")
    /// If null, will be auto-generated as plural of SourceEntity
    /// </summary>
    public string? InverseNavigation { get; set; }

    /// <summary>
    /// For many-to-many: Name of the junction table (e.g., "PostTag")
    /// If null, will be auto-generated alphabetically
    /// </summary>
    public string? JunctionTable { get; set; }

    /// <summary>
    /// For many-to-many: Additional properties for the junction table
    /// Key = property name, Value = property type
    /// Example: { "CreatedAt": "datetime", "CreatedBy": "string" }
    /// </summary>
    public Dictionary<string, string>? JunctionProperties { get; set; }

    /// <summary>
    /// For one-to-one: The principal entity (owns the relationship)
    /// If null, will be auto-detected
    /// </summary>
    public string? PrincipalEntity { get; set; }

    /// <summary>
    /// For one-to-one: The dependent entity (has the foreign key)
    /// If null, will be auto-detected
    /// </summary>
    public string? DependentEntity { get; set; }

    /// <summary>
    /// Whether to skip navigation property generation
    /// </summary>
    public bool SkipNavigation { get; set; }

    /// <summary>
    /// Whether to skip UI generation (forms, views)
    /// </summary>
    public bool SkipUI { get; set; }

    /// <summary>
    /// Whether to skip automatic migration creation
    /// </summary>
    public bool SkipMigrations { get; set; }

    /// <summary>
    /// Project directory path
    /// </summary>
    public string ProjectPath { get; set; } = string.Empty;
}
