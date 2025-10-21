using System.Text;
using NetMX.CLI.Models;

namespace NetMX.CLI.Infrastructure;

/// <summary>
/// Generates ASP.NET Core MVC controllers with HTMX support for CRUD operations.
/// </summary>
public static class ControllerGenerator
{
    /// <summary>
    /// Generates controller with HTMX-optimized actions.
    /// </summary>
    public static string GenerateController(EntityGenerationOptions options)
    {
        var sb = new StringBuilder();

        // Namespace
        var namespaceName = options.ModuleName != null
            ? $"{options.ModuleName}.Web.Controllers"
            : "Controllers";

        sb.AppendLine($"namespace {namespaceName};");
        sb.AppendLine();

        // Using statements
        sb.AppendLine("using Microsoft.AspNetCore.Mvc;");
        
        var contractsNamespace = options.ModuleName != null
            ? $"{options.ModuleName}.Contracts"
            : "Contracts";
        sb.AppendLine($"using {contractsNamespace}.Dtos;");
        sb.AppendLine($"using {contractsNamespace}.Services;");
        sb.AppendLine("using NetMX.AspNetCore.Mvc.Htmx;");
        sb.AppendLine();

        // Controller class
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Controller for {options.EntityName} CRUD operations with HTMX support");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public class {options.EntityName}Controller : Controller");
        sb.AppendLine("{");

        // Field
        sb.AppendLine($"    private readonly I{options.EntityName}Service _service;");
        sb.AppendLine();

        // Constructor
        sb.AppendLine($"    public {options.EntityName}Controller(I{options.EntityName}Service service)");
        sb.AppendLine("    {");
        sb.AppendLine("        _service = service;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Index action
        GenerateIndexAction(sb, options);

        // List action (HTMX partial)
        GenerateListAction(sb, options);

        // Create actions
        GenerateCreateActions(sb, options);

        // Edit actions
        GenerateEditActions(sb, options);

        // Delete action
        GenerateDeleteAction(sb, options);

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void GenerateIndexAction(StringBuilder sb, EntityGenerationOptions options)
    {
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Displays the {options.EntityName} list page");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    [HttpGet]");
        sb.AppendLine("    public IActionResult Index()");
        sb.AppendLine("    {");
        sb.AppendLine("        return View();");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void GenerateListAction(StringBuilder sb, EntityGenerationOptions options)
    {
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Gets {options.EntityName} list (HTMX partial)");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    [HttpGet]");
        
        if (options.HasPagination || options.HasFilters || options.HasSearch || options.HasSorting)
        {
            sb.AppendLine("    public async Task<IActionResult> List(");
            
            if (options.HasSearch)
            {
                sb.AppendLine("        string? searchQuery = null,");
            }
            
            if (options.HasFilters)
            {
                foreach (var filterProp in options.FilterableProperties)
                {
                    if (filterProp.EndsWith("Range", StringComparison.OrdinalIgnoreCase))
                    {
                        var baseName = filterProp[..^5];
                        var propName = char.ToUpper(baseName[0]) + baseName[1..];
                        var prop = options.Properties.FirstOrDefault(p => 
                            p.Name.Equals(baseName, StringComparison.OrdinalIgnoreCase));
                        
                        if (prop != null)
                        {
                            sb.AppendLine($"        {prop.Type}? {baseName}Min = null,");
                            sb.AppendLine($"        {prop.Type}? {baseName}Max = null,");
                        }
                    }
                    else
                    {
                        var prop = options.Properties.FirstOrDefault(p => 
                            p.Name.Equals(filterProp, StringComparison.OrdinalIgnoreCase));
                        
                        if (prop != null)
                        {
                            sb.AppendLine($"        {prop.Type}? {filterProp} = null,");
                        }
                    }
                }
            }
            
            if (options.HasPagination)
            {
                sb.AppendLine("        int pageNumber = 1,");
                sb.AppendLine($"        int pageSize = {options.PageSize ?? 20},");
            }
            
            if (options.HasSorting)
            {
                sb.AppendLine("        string? sortBy = null,");
                sb.AppendLine("        bool sortDescending = false)");
            }
            else
            {
                // Remove last comma
                var currentText = sb.ToString();
                var lastCommaIndex = currentText.LastIndexOf(',');
                if (lastCommaIndex > 0)
                {
                    sb.Length = lastCommaIndex;
                    sb.AppendLine(")");
                }
            }
        }
        else
        {
            sb.AppendLine("    public async Task<IActionResult> List()");
        }
        
        sb.AppendLine("    {");
        
        if (options.HasPagination || options.HasFilters || options.HasSearch)
        {
            // Build filter DTO
            sb.AppendLine($"        var filter = new {options.EntityName}FilterDto");
            sb.AppendLine("        {");
            
            if (options.HasSearch)
            {
                sb.AppendLine("            SearchQuery = searchQuery,");
            }
            
            if (options.HasFilters)
            {
                foreach (var filterProp in options.FilterableProperties)
                {
                    if (filterProp.EndsWith("Range", StringComparison.OrdinalIgnoreCase))
                    {
                        var baseName = filterProp[..^5];
                        var propName = char.ToUpper(baseName[0]) + baseName[1..];
                        sb.AppendLine($"            {propName}Min = {baseName}Min,");
                        sb.AppendLine($"            {propName}Max = {baseName}Max,");
                    }
                    else
                    {
                        var propName = char.ToUpper(filterProp[0]) + filterProp[1..];
                        sb.AppendLine($"            {propName} = {filterProp},");
                    }
                }
            }
            
            sb.AppendLine("        };");
            sb.AppendLine();
            
            sb.AppendLine("        var result = await _service.GetAllAsync(filter, pageNumber, pageSize, sortBy, sortDescending);");
            sb.AppendLine();
            sb.AppendLine("        return PartialView(\"_List\", result);");
        }
        else
        {
            sb.AppendLine("        var items = await _service.GetAllAsync();");
            sb.AppendLine();
            sb.AppendLine("        return PartialView(\"_List\", items);");
        }
        
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void GenerateCreateActions(StringBuilder sb, EntityGenerationOptions options)
    {
        // GET Create (form)
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Shows create {options.EntityName} form (HTMX partial)");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    [HttpGet]");
        sb.AppendLine("    public IActionResult Create()");
        sb.AppendLine("    {");
        sb.AppendLine($"        var dto = new Create{options.EntityName}Dto();");
        sb.AppendLine("        return PartialView(\"_Form\", dto);");
        sb.AppendLine("    }");
        sb.AppendLine();

        // POST Create
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Creates a new {options.EntityName} (HTMX)");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    [HttpPost]");
        sb.AppendLine($"    public async Task<IActionResult> Create(Create{options.EntityName}Dto dto)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (!ModelState.IsValid)");
        sb.AppendLine("        {");
        sb.AppendLine("            return PartialView(\"_Form\", dto);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        await _service.CreateAsync(dto);");
        sb.AppendLine();
        sb.AppendLine("        // Trigger HTMX event to refresh list and close modal");
        sb.AppendLine($"        this.HxTrigger(\"{options.EntityName.ToLower()}-created\");");
        sb.AppendLine();
        sb.AppendLine("        return Ok();");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void GenerateEditActions(StringBuilder sb, EntityGenerationOptions options)
    {
        // GET Edit (form)
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Shows edit {options.EntityName} form (HTMX partial)");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    [HttpGet]");
        sb.AppendLine($"    public async Task<IActionResult> Edit({options.KeyType} id)");
        sb.AppendLine("    {");
        sb.AppendLine("        var entity = await _service.GetByIdAsync(id);");
        sb.AppendLine();
        sb.AppendLine("        if (entity == null)");
        sb.AppendLine("        {");
        sb.AppendLine("            return NotFound();");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine($"        var dto = new Update{options.EntityName}Dto");
        sb.AppendLine("        {");
        sb.AppendLine("            Id = entity.Id,");
        
        foreach (var prop in options.Properties.Where(p => !p.IsCollection))
        {
            var propName = char.ToUpper(prop.Name[0]) + prop.Name[1..];
            sb.AppendLine($"            {propName} = entity.{propName},");
        }
        
        sb.AppendLine("        };");
        sb.AppendLine();
        sb.AppendLine("        return PartialView(\"_Form\", dto);");
        sb.AppendLine("    }");
        sb.AppendLine();

        // POST Edit
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Updates an existing {options.EntityName} (HTMX)");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    [HttpPost]");
        sb.AppendLine($"    public async Task<IActionResult> Edit(Update{options.EntityName}Dto dto)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (!ModelState.IsValid)");
        sb.AppendLine("        {");
        sb.AppendLine("            return PartialView(\"_Form\", dto);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        await _service.UpdateAsync(dto);");
        sb.AppendLine();
        sb.AppendLine("        // Trigger HTMX event to refresh list and close modal");
        sb.AppendLine($"        this.HxTrigger(\"{options.EntityName.ToLower()}-updated\");");
        sb.AppendLine();
        sb.AppendLine("        return Ok();");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void GenerateDeleteAction(StringBuilder sb, EntityGenerationOptions options)
    {
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Deletes a {options.EntityName} (HTMX)");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    [HttpDelete]");
        sb.AppendLine($"    public async Task<IActionResult> Delete({options.KeyType} id)");
        sb.AppendLine("    {");
        sb.AppendLine("        await _service.DeleteAsync(id);");
        sb.AppendLine();
        sb.AppendLine("        // Trigger HTMX event to refresh list");
        sb.AppendLine($"        this.HxTrigger(\"{options.EntityName.ToLower()}-deleted\");");
        sb.AppendLine();
        sb.AppendLine("        // Use HTMX swap to remove the row");
        sb.AppendLine("        this.HxReswap(HtmxSwap.Delete);");
        sb.AppendLine();
        sb.AppendLine("        return Ok();");
        sb.AppendLine("    }");
    }
}
