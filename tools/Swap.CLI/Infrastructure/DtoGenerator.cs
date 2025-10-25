using System.Text;
using Swap.CLI.Models;

namespace Swap.CLI.Infrastructure;

/// <summary>
/// Generates DTOs (Data Transfer Objects) for entities.
/// Creates ReadDto, CreateDto, UpdateDto, FilterDto, and PagedResultDto.
/// </summary>
public class DtoGenerator
{
    /// <summary>
    /// Generates ReadDto (for retrieving data).
    /// </summary>
    public static string GenerateReadDto(EntityGenerationOptions options)
    {
        var sb = new StringBuilder();

        // Usings
        sb.AppendLine("using System.ComponentModel.DataAnnotations;");
        sb.AppendLine();

        // Namespace
        var namespaceName = options.ModuleName != null
            ? $"{options.ModuleName}.Contracts.Dtos"
            : options.ProjectNamespace != null
                ? $"{options.ProjectNamespace}.Dtos"
                : "Dtos";

        sb.AppendLine($"namespace {namespaceName};");
        sb.AppendLine();

        // Class header
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// {options.EntityName} read DTO");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public class {options.EntityName}Dto");
        sb.AppendLine("{");

        // Id property
        sb.AppendLine($"    public {options.KeyType} Id {{ get; set; }}");
        sb.AppendLine();

        // Properties (read-only, no validation)
        foreach (var prop in options.Properties)
        {
            var nullable = prop.IsNullable ? "?" : "";
            sb.AppendLine($"    public {prop.Type}{nullable} {prop.Name} {{ get; set; }}");
        }

        // Audit fields
        if (options.IncludeAuditFields)
        {
            sb.AppendLine();
            sb.AppendLine("    public DateTime CreatedAt { get; set; }");
            sb.AppendLine("    public DateTime? UpdatedAt { get; set; }");
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Generates CreateDto (for creating new entities).
    /// </summary>
    public static string GenerateCreateDto(EntityGenerationOptions options)
    {
        var sb = new StringBuilder();

        // Usings
        sb.AppendLine("using System.ComponentModel.DataAnnotations;");
        sb.AppendLine();

        // Namespace
        var namespaceName = options.ModuleName != null
            ? $"{options.ModuleName}.Contracts.Dtos"
            : options.ProjectNamespace != null
                ? $"{options.ProjectNamespace}.Dtos"
                : "Dtos";

        sb.AppendLine($"namespace {namespaceName};");
        sb.AppendLine();

        // Class header
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// {options.EntityName} create DTO");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public class Create{options.EntityName}Dto");
        sb.AppendLine("{");

        // Properties (with validation attributes)
        foreach (var prop in options.Properties)
        {
            // Skip navigation collections
            if (prop.IsCollection && prop.IsNavigationProperty)
                continue;

            var nullable = prop.IsNullable ? "?" : "";

            // Add validation attributes
            if (prop.IsRequired)
            {
                sb.AppendLine("    [Required]");
            }

            if (prop.MaxLength.HasValue)
            {
                sb.AppendLine($"    [MaxLength({prop.MaxLength.Value})]");
            }

            if (prop.MinValue != null || prop.MaxValue != null)
            {
                var min = prop.MinValue ?? "0";
                var max = prop.MaxValue ?? "double.MaxValue";
                sb.AppendLine($"    [Range({min}, {max})]");
            }

            sb.AppendLine($"    public {prop.Type}{nullable} {prop.Name} {{ get; set; }}");
            sb.AppendLine();
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Generates UpdateDto (for updating entities).
    /// </summary>
    public static string GenerateUpdateDto(EntityGenerationOptions options)
    {
        var sb = new StringBuilder();

        // Usings
        sb.AppendLine("using System.ComponentModel.DataAnnotations;");
        sb.AppendLine();

        // Namespace
        var namespaceName = options.ModuleName != null
            ? $"{options.ModuleName}.Contracts.Dtos"
            : options.ProjectNamespace != null
                ? $"{options.ProjectNamespace}.Dtos"
                : "Dtos";

        sb.AppendLine($"namespace {namespaceName};");
        sb.AppendLine();

        // Class header
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// {options.EntityName} update DTO");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public class Update{options.EntityName}Dto");
        sb.AppendLine("{");

        // Id property
        sb.AppendLine($"    public {options.KeyType} Id {{ get; set; }}");
        sb.AppendLine();

        // Properties (with validation attributes)
        foreach (var prop in options.Properties)
        {
            // Skip navigation collections
            if (prop.IsCollection && prop.IsNavigationProperty)
                continue;

            var nullable = prop.IsNullable ? "?" : "";

            // Add validation attributes
            if (prop.IsRequired)
            {
                sb.AppendLine("    [Required]");
            }

            if (prop.MaxLength.HasValue)
            {
                sb.AppendLine($"    [MaxLength({prop.MaxLength.Value})]");
            }

            if (prop.MinValue != null || prop.MaxValue != null)
            {
                var min = prop.MinValue ?? "0";
                var max = prop.MaxValue ?? "double.MaxValue";
                sb.AppendLine($"    [Range({min}, {max})]");
            }

            sb.AppendLine($"    public {prop.Type}{nullable} {prop.Name} {{ get; set; }}");
            sb.AppendLine();
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Generates FilterDto (for filtering/searching).
    /// </summary>
    public static string GenerateFilterDto(EntityGenerationOptions options)
    {
        if (!options.HasFilters && !options.HasSearch)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();

        // Namespace
        var namespaceName = options.ModuleName != null
            ? $"{options.ModuleName}.Contracts.Dtos"
            : options.ProjectNamespace != null
                ? $"{options.ProjectNamespace}.Dtos"
                : "Dtos";

        sb.AppendLine($"namespace {namespaceName};");
        sb.AppendLine();

        // Class header
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// {options.EntityName} filter DTO");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public class {options.EntityName}FilterDto");
        sb.AppendLine("{");

        // Search query (if enabled)
        if (options.HasSearch)
        {
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// Search query (searches: {string.Join(", ", options.SearchableProperties)})");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public string? SearchQuery { get; set; }");
            sb.AppendLine();
        }

        // Filter properties
        foreach (var filterProp in options.FilterableProperties)
        {
            var prop = options.Properties.FirstOrDefault(p => 
                p.Name.Equals(filterProp, StringComparison.OrdinalIgnoreCase));

            if (prop != null)
            {
                sb.AppendLine($"    public {prop.Type}? {prop.Name} {{ get; set; }}");
            }
            else
            {
                // Range filters (e.g., priceRange, dateRange)
                if (filterProp.EndsWith("Range", StringComparison.OrdinalIgnoreCase))
                {
                    var baseName = filterProp[..^5]; // Remove "Range"
                    var baseProp = options.Properties.FirstOrDefault(p =>
                        p.Name.Equals(baseName, StringComparison.OrdinalIgnoreCase));

                    if (baseProp != null)
                    {
                        // Capitalize first letter for property name (priceRange → PriceMin/PriceMax)
                        var propName = char.ToUpper(baseProp.Name[0]) + baseProp.Name[1..];
                        sb.AppendLine($"    public {baseProp.Type}? {propName}Min {{ get; set; }}");
                        sb.AppendLine($"    public {baseProp.Type}? {propName}Max {{ get; set; }}");
                    }
                }
            }
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Generates PagedResultDto (for pagination).
    /// </summary>
    public static string GeneratePagedResultDto(EntityGenerationOptions options)
    {
        if (!options.HasPagination)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();

        // Namespace
        var namespaceName = options.ModuleName != null
            ? $"{options.ModuleName}.Contracts.Dtos"
            : options.ProjectNamespace != null
                ? $"{options.ProjectNamespace}.Dtos"
                : "Dtos";

        sb.AppendLine($"namespace {namespaceName};");
        sb.AppendLine();

        // Class header
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Paged result for {options.EntityName}");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public class Paged{options.EntityName}ResultDto");
        sb.AppendLine("{");

        sb.AppendLine($"    public List<{options.EntityName}Dto> Items {{ get; set; }} = new();");
        sb.AppendLine("    public int TotalCount { get; set; }");
        sb.AppendLine("    public int PageNumber { get; set; }");
        sb.AppendLine("    public int PageSize { get; set; }");
        sb.AppendLine("    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);");
        sb.AppendLine("    public bool HasPreviousPage => PageNumber > 1;");
        sb.AppendLine("    public bool HasNextPage => PageNumber < TotalPages;");

        sb.AppendLine("}");

        return sb.ToString();
    }
}

