using System.Text;
using Swap.CLI.Models;

namespace Swap.CLI.Infrastructure;

/// <summary>
/// Generates service interface and implementation with CRUD operations, pagination, search, filter, and sort.
/// </summary>
public static class ServiceGenerator
{
    /// <summary>
    /// Generates service interface (IEntityService).
    /// </summary>
    public static string GenerateServiceInterface(EntityGenerationOptions options)
    {
        var sb = new StringBuilder();

        // Namespace
        var namespaceName = options.ModuleName != null
            ? $"{options.ModuleName}.Contracts.Services"
            : options.ProjectNamespace != null
                ? $"{options.ProjectNamespace}.Services"
                : "Services";

        sb.AppendLine($"namespace {namespaceName};");
        sb.AppendLine();

        // Using statements
        var dtoNamespace = options.ModuleName != null
            ? $"{options.ModuleName}.Contracts.Dtos"
            : options.ProjectNamespace != null
                ? $"{options.ProjectNamespace}.Dtos"
                : "Dtos";
        sb.AppendLine($"using {dtoNamespace};");
        sb.AppendLine();

        // Interface
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Service interface for {options.EntityName} operations");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public interface I{options.EntityName}Service");
        sb.AppendLine("{");

        // GetAll method (with pagination/filter support)
        if (options.HasPagination || options.HasFilters || options.HasSearch)
        {
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// Gets paginated and filtered {options.EntityName} list");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    Task<Paged{options.EntityName}ResultDto> GetAllAsync(");
            sb.AppendLine($"        {options.EntityName}FilterDto filter,");
            sb.AppendLine("        int pageNumber = 1,");
            sb.AppendLine($"        int pageSize = {options.PageSize ?? 20},");
            sb.AppendLine("        string? sortBy = null,");
            sb.AppendLine("        bool sortDescending = false);");
        }
        else
        {
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// Gets all {options.EntityName}s");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    Task<List<{options.EntityName}Dto>> GetAllAsync();");
        }
        sb.AppendLine();

        // GetById
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Gets {options.EntityName} by ID");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine($"    Task<{options.EntityName}Dto?> GetByIdAsync({options.KeyType} id);");
        sb.AppendLine();

        // Create
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Creates a new {options.EntityName}");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine($"    Task<{options.EntityName}Dto> CreateAsync(Create{options.EntityName}Dto dto);");
        sb.AppendLine();

        // Update
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Updates an existing {options.EntityName}");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine($"    Task<{options.EntityName}Dto> UpdateAsync(Update{options.EntityName}Dto dto);");
        sb.AppendLine();

        // Delete
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Deletes {options.EntityName} by ID");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine($"    Task DeleteAsync({options.KeyType} id);");

        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Generates service implementation (EntityService).
    /// </summary>
    public static string GenerateServiceImplementation(EntityGenerationOptions options)
    {
        var sb = new StringBuilder();

        // Namespace
        var namespaceName = options.ModuleName != null
            ? $"{options.ModuleName}.Application.Services"
            : options.ProjectNamespace != null
                ? $"{options.ProjectNamespace}.Services"
                : "Services";

        sb.AppendLine($"namespace {namespaceName};");
        sb.AppendLine();

        // Using statements
        sb.AppendLine("using Microsoft.EntityFrameworkCore;");
        var contractsNamespace = options.ModuleName != null
            ? $"{options.ModuleName}.Contracts"
            : options.ProjectNamespace;
        
        if (contractsNamespace != null)
        {
            sb.AppendLine($"using {contractsNamespace}.Dtos;");
            sb.AppendLine($"using {contractsNamespace}.Services;");
        }
        
        var modelsNamespace = options.ModuleName != null
            ? $"{options.ModuleName}.Core.Entities"
            : options.ProjectNamespace != null
                ? $"{options.ProjectNamespace}.Models"
                : "Models";
        sb.AppendLine($"using {modelsNamespace};");
        
        // Add Data namespace for DbContext
        var dataNamespace = options.ModuleName != null
            ? $"{options.ModuleName}.Infrastructure.Data"
            : options.ProjectNamespace != null
                ? $"{options.ProjectNamespace}.Data"
                : "Data";
        sb.AppendLine($"using {dataNamespace};");
        sb.AppendLine();

        // Class
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Service implementation for {options.EntityName} operations");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public class {options.EntityName}Service : I{options.EntityName}Service");
        sb.AppendLine("{");
        
        // Determine DbContext name
        var dbContextName = options.ModuleName != null
            ? $"{options.ModuleName}DbContext"
            : options.ProjectNamespace != null
                ? "AppDbContext"
                : "AppDbContext";
        
        // Fields
        sb.AppendLine($"    private readonly {dbContextName} _context;");
        sb.AppendLine();

        // Constructor
        sb.AppendLine($"    public {options.EntityName}Service({dbContextName} context)");
        sb.AppendLine("    {");
        sb.AppendLine("        _context = context;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // GetAll method
        GenerateGetAllMethod(sb, options);

        // GetById method
        GenerateGetByIdMethod(sb, options);

        // Create method
        GenerateCreateMethod(sb, options);

        // Update method
        GenerateUpdateMethod(sb, options);

        // Delete method
        GenerateDeleteMethod(sb, options);

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void GenerateGetAllMethod(StringBuilder sb, EntityGenerationOptions options)
    {
        if (options.HasPagination || options.HasFilters || options.HasSearch)
        {
            sb.AppendLine("    /// <inheritdoc />");
            sb.AppendLine($"    public async Task<Paged{options.EntityName}ResultDto> GetAllAsync(");
            sb.AppendLine($"        {options.EntityName}FilterDto filter,");
            sb.AppendLine("        int pageNumber = 1,");
            sb.AppendLine($"        int pageSize = {options.PageSize ?? 20},");
            sb.AppendLine("        string? sortBy = null,");
            sb.AppendLine("        bool sortDescending = false)");
            sb.AppendLine("    {");
            sb.AppendLine($"        var query = _context.Set<{options.EntityName}>().AsQueryable();");
            sb.AppendLine();

            // Apply search
            if (options.HasSearch)
            {
                sb.AppendLine("        // Apply search");
                sb.AppendLine("        if (!string.IsNullOrWhiteSpace(filter.SearchQuery))");
                sb.AppendLine("        {");
                sb.AppendLine("            var search = filter.SearchQuery.ToLower();");
                sb.AppendLine("            query = query.Where(x => ");

                var searchConditions = options.SearchableProperties
                    .Select(prop => $"x.{char.ToUpper(prop[0]) + prop[1..]}.ToLower().Contains(search)")
                    .ToList();

                for (int i = 0; i < searchConditions.Count; i++)
                {
                    var isLast = i == searchConditions.Count - 1;
                    sb.Append($"                {searchConditions[i]}");
                    if (!isLast)
                    {
                        sb.AppendLine(" ||");
                    }
                    else
                    {
                        sb.AppendLine(");");
                    }
                }
                sb.AppendLine("        }");
                sb.AppendLine();
            }

            // Apply filters
            if (options.HasFilters)
            {
                sb.AppendLine("        // Apply filters");
                foreach (var filterProp in options.FilterableProperties)
                {
                    // Skip range filters (handled separately)
                    if (filterProp.EndsWith("Range", StringComparison.OrdinalIgnoreCase))
                    {
                        var baseName = filterProp[..^5];
                        var propName = char.ToUpper(baseName[0]) + baseName[1..];
                        
                        sb.AppendLine($"        if (filter.{propName}Min.HasValue)");
                        sb.AppendLine($"            query = query.Where(x => x.{propName} >= filter.{propName}Min.Value);");
                        sb.AppendLine();
                        sb.AppendLine($"        if (filter.{propName}Max.HasValue)");
                        sb.AppendLine($"            query = query.Where(x => x.{propName} <= filter.{propName}Max.Value);");
                        sb.AppendLine();
                    }
                    else
                    {
                        var propName = char.ToUpper(filterProp[0]) + filterProp[1..];
                        sb.AppendLine($"        if (filter.{propName}.HasValue)");
                        sb.AppendLine($"            query = query.Where(x => x.{propName} == filter.{propName}.Value);");
                        sb.AppendLine();
                    }
                }
            }

            // Apply sorting
            if (options.HasSorting)
            {
                sb.AppendLine("        // Apply sorting");
                sb.AppendLine("        if (!string.IsNullOrWhiteSpace(sortBy))");
                sb.AppendLine("        {");
                sb.AppendLine("            query = sortBy.ToLower() switch");
                sb.AppendLine("            {");
                
                foreach (var sortProp in options.SortableProperties)
                {
                    var propName = char.ToUpper(sortProp[0]) + sortProp[1..];
                    sb.AppendLine($"                \"{sortProp.ToLower()}\" => sortDescending");
                    sb.AppendLine($"                    ? query.OrderByDescending(x => x.{propName})");
                    sb.AppendLine($"                    : query.OrderBy(x => x.{propName}),");
                }
                
                sb.AppendLine("                _ => query");
                sb.AppendLine("            };");
                sb.AppendLine("        }");
                sb.AppendLine();
            }

            // Get total count
            sb.AppendLine("        var totalCount = await query.CountAsync();");
            sb.AppendLine();

            // Apply pagination
            sb.AppendLine("        // Apply pagination");
            sb.AppendLine("        var items = await query");
            sb.AppendLine("            .Skip((pageNumber - 1) * pageSize)");
            sb.AppendLine("            .Take(pageSize)");
            sb.AppendLine("            .Select(x => new " + options.EntityName + "Dto");
            sb.AppendLine("            {");
            sb.AppendLine("                Id = x.Id,");
            
            foreach (var prop in options.Properties.Where(p => !p.IsCollection))
            {
                var propName = char.ToUpper(prop.Name[0]) + prop.Name[1..];
                sb.AppendLine($"                {propName} = x.{propName},");
            }
            
            if (options.IncludeAuditFields)
            {
                sb.AppendLine("                CreatedAt = x.CreatedAt,");
                sb.AppendLine("                UpdatedAt = x.UpdatedAt");
            }
            else
            {
                // Remove last comma
                var lastLine = sb.ToString().TrimEnd();
                if (lastLine.EndsWith(","))
                {
                    sb.Length -= 3; // Remove ",\r\n"
                    sb.AppendLine();
                }
            }
            
            sb.AppendLine("            })");
            sb.AppendLine("            .ToListAsync();");
            sb.AppendLine();

            // Return paged result
            sb.AppendLine($"        return new Paged{options.EntityName}ResultDto");
            sb.AppendLine("        {");
            sb.AppendLine("            Items = items,");
            sb.AppendLine("            TotalCount = totalCount,");
            sb.AppendLine("            PageNumber = pageNumber,");
            sb.AppendLine("            PageSize = pageSize");
            sb.AppendLine("        };");
            sb.AppendLine("    }");
        }
        else
        {
            sb.AppendLine("    /// <inheritdoc />");
            sb.AppendLine($"    public async Task<List<{options.EntityName}Dto>> GetAllAsync()");
            sb.AppendLine("    {");
            sb.AppendLine($"        return await _context.Set<{options.EntityName}>()");
            sb.AppendLine("            .Select(x => new " + options.EntityName + "Dto");
            sb.AppendLine("            {");
            sb.AppendLine("                Id = x.Id,");
            
            foreach (var prop in options.Properties.Where(p => !p.IsCollection))
            {
                var propName = char.ToUpper(prop.Name[0]) + prop.Name[1..];
                sb.AppendLine($"                {propName} = x.{propName},");
            }
            
            if (options.IncludeAuditFields)
            {
                sb.AppendLine("                CreatedAt = x.CreatedAt,");
                sb.AppendLine("                UpdatedAt = x.UpdatedAt");
            }
            else
            {
                // Remove last comma
                var lastLine = sb.ToString().TrimEnd();
                if (lastLine.EndsWith(","))
                {
                    sb.Length -= 3;
                    sb.AppendLine();
                }
            }
            
            sb.AppendLine("            })");
            sb.AppendLine("            .ToListAsync();");
            sb.AppendLine("    }");
        }
        sb.AppendLine();
    }

    private static void GenerateGetByIdMethod(StringBuilder sb, EntityGenerationOptions options)
    {
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine($"    public async Task<{options.EntityName}Dto?> GetByIdAsync({options.KeyType} id)");
        sb.AppendLine("    {");
        sb.AppendLine($"        var entity = await _context.Set<{options.EntityName}>()");
        sb.AppendLine("            .FirstOrDefaultAsync(x => x.Id == id);");
        sb.AppendLine();
        sb.AppendLine("        if (entity == null)");
        sb.AppendLine("            return null;");
        sb.AppendLine();
        sb.AppendLine($"        return new {options.EntityName}Dto");
        sb.AppendLine("        {");
        sb.AppendLine("            Id = entity.Id,");
        
        foreach (var prop in options.Properties.Where(p => !p.IsCollection))
        {
            var propName = char.ToUpper(prop.Name[0]) + prop.Name[1..];
            sb.AppendLine($"            {propName} = entity.{propName},");
        }
        
        if (options.IncludeAuditFields)
        {
            sb.AppendLine("            CreatedAt = entity.CreatedAt,");
            sb.AppendLine("            UpdatedAt = entity.UpdatedAt");
        }
        else
        {
            // Remove last comma
            var lastLine = sb.ToString().TrimEnd();
            if (lastLine.EndsWith(","))
            {
                sb.Length -= 3;
                sb.AppendLine();
            }
        }
        
        sb.AppendLine("        };");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void GenerateCreateMethod(StringBuilder sb, EntityGenerationOptions options)
    {
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine($"    public async Task<{options.EntityName}Dto> CreateAsync(Create{options.EntityName}Dto dto)");
        sb.AppendLine("    {");
        
        // Constructor parameters (only required properties)
        var requiredProps = options.Properties
            .Where(p => p.IsRequired && !p.IsCollection)
            .ToList();
        
        sb.Append($"        var entity = new {options.EntityName}(");
        if (options.KeyType == "Guid")
        {
            sb.Append("Guid.NewGuid()");
            if (requiredProps.Any())
                sb.Append(", ");
        }
        
        for (int i = 0; i < requiredProps.Count; i++)
        {
            var prop = requiredProps[i];
            var propName = char.ToUpper(prop.Name[0]) + prop.Name[1..];
            sb.Append($"dto.{propName}");
            if (i < requiredProps.Count - 1)
                sb.Append(", ");
        }
        sb.AppendLine(");");
        sb.AppendLine();

        // Set optional properties
        var optionalProps = options.Properties
            .Where(p => !p.IsRequired && !p.IsCollection)
            .ToList();

        if (optionalProps.Any())
        {
            sb.AppendLine("        // Set optional properties");
            foreach (var prop in optionalProps)
            {
                var propName = char.ToUpper(prop.Name[0]) + prop.Name[1..];
                sb.AppendLine($"        entity.Set{propName}(dto.{propName});");
            }
            sb.AppendLine();
        }

        sb.AppendLine($"        _context.Set<{options.EntityName}>().Add(entity);");
        sb.AppendLine("        await _context.SaveChangesAsync();");
        sb.AppendLine();
        sb.AppendLine("        return await GetByIdAsync(entity.Id) ?? throw new InvalidOperationException(\"Failed to retrieve created entity\");");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void GenerateUpdateMethod(StringBuilder sb, EntityGenerationOptions options)
    {
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine($"    public async Task<{options.EntityName}Dto> UpdateAsync(Update{options.EntityName}Dto dto)");
        sb.AppendLine("    {");
        sb.AppendLine($"        var entity = await _context.Set<{options.EntityName}>()");
        sb.AppendLine("            .FirstOrDefaultAsync(x => x.Id == dto.Id);");
        sb.AppendLine();
        sb.AppendLine("        if (entity == null)");
        sb.AppendLine($"            throw new InvalidOperationException(\"{options.EntityName} not found\");");
        sb.AppendLine();

        // Update all properties using Set methods
        foreach (var prop in options.Properties.Where(p => !p.IsCollection))
        {
            var propName = char.ToUpper(prop.Name[0]) + prop.Name[1..];
            sb.AppendLine($"        entity.Set{propName}(dto.{propName});");
        }

        sb.AppendLine();
        sb.AppendLine("        await _context.SaveChangesAsync();");
        sb.AppendLine();
        sb.AppendLine("        return await GetByIdAsync(entity.Id) ?? throw new InvalidOperationException(\"Failed to retrieve updated entity\");");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void GenerateDeleteMethod(StringBuilder sb, EntityGenerationOptions options)
    {
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine($"    public async Task DeleteAsync({options.KeyType} id)");
        sb.AppendLine("    {");
        sb.AppendLine($"        var entity = await _context.Set<{options.EntityName}>()");
        sb.AppendLine("            .FirstOrDefaultAsync(x => x.Id == id);");
        sb.AppendLine();
        sb.AppendLine("        if (entity == null)");
        sb.AppendLine($"            throw new InvalidOperationException(\"{options.EntityName} not found\");");
        sb.AppendLine();

        if (options.IncludeSoftDelete)
        {
            sb.AppendLine("        // Soft delete");
            sb.AppendLine("        entity.Delete();");
        }
        else
        {
            sb.AppendLine("        // Hard delete");
            sb.AppendLine($"        _context.Set<{options.EntityName}>().Remove(entity);");
        }

        sb.AppendLine("        await _context.SaveChangesAsync();");
        sb.AppendLine("    }");
    }
}

