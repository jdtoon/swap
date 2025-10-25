namespace Swap.CLI.Models;

/// <summary>
/// Options for entity generation from CLI.
/// Captures all flags like --paginate, --search, --filter, --sort, --export.
/// </summary>
public class EntityGenerationOptions
{
    /// <summary>
    /// Entity name (e.g., "Product")
    /// </summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// Module name (optional, e.g., "Catalog")
    /// </summary>
    public string? ModuleName { get; set; }

    /// <summary>
    /// Property definitions parsed from CLI
    /// </summary>
    public List<PropertyDefinition> Properties { get; set; } = new();

    /// <summary>
    /// Enable pagination with specified page size (e.g., --paginate:20)
    /// </summary>
    public int? PageSize { get; set; }

    /// <summary>
    /// Enable search on specified properties (e.g., --search:name,description)
    /// </summary>
    public List<string> SearchableProperties { get; set; } = new();

    /// <summary>
    /// Enable filtering on specified properties (e.g., --filter:category,status,priceRange)
    /// </summary>
    public List<string> FilterableProperties { get; set; } = new();

    /// <summary>
    /// Enable sorting on specified properties (e.g., --sort:name,price,createdAt)
    /// </summary>
    public List<string> SortableProperties { get; set; } = new();

    /// <summary>
    /// Enable export formats (e.g., --export:csv,excel,pdf)
    /// </summary>
    public List<string> ExportFormats { get; set; } = new();

    /// <summary>
    /// Auto-create migration (--migrate flag)
    /// </summary>
    public bool AutoMigrate { get; set; }

    /// <summary>
    /// Generate audit fields (CreatedAt, UpdatedAt, etc.)
    /// Default: true
    /// </summary>
    public bool IncludeAuditFields { get; set; } = true;

    /// <summary>
    /// Generate soft delete (IsDeleted, DeletedAt)
    /// Default: false
    /// </summary>
    public bool IncludeSoftDelete { get; set; } = false;

    /// <summary>
    /// Base class for entity (default: AggregateRoot<Guid>)
    /// </summary>
    public string BaseClass { get; set; } = "AggregateRoot<Guid>";

    /// <summary>
    /// Key type (default: Guid)
    /// </summary>
    public string KeyType { get; set; } = "Guid";

    /// <summary>
    /// Project root namespace (e.g., "ECommerce.Web")
    /// Detected from .csproj file or inferred from project name
    /// </summary>
    public string? ProjectNamespace { get; set; }

    /// <summary>
    /// Check if pagination is enabled
    /// </summary>
    public bool HasPagination => PageSize.HasValue && PageSize > 0;

    /// <summary>
    /// Check if search is enabled
    /// </summary>
    public bool HasSearch => SearchableProperties.Count > 0;

    /// <summary>
    /// Check if filtering is enabled
    /// </summary>
    public bool HasFilters => FilterableProperties.Count > 0;

    /// <summary>
    /// Check if sorting is enabled
    /// </summary>
    public bool HasSorting => SortableProperties.Count > 0;

    /// <summary>
    /// Check if export is enabled
    /// </summary>
    public bool HasExport => ExportFormats.Count > 0;

    /// <summary>
    /// Check if any advanced features are enabled
    /// </summary>
    public bool HasAdvancedFeatures => HasPagination || HasSearch || HasFilters || HasSorting || HasExport;
}

