using System.Text;
using NetMX.CLI.Models;

namespace NetMX.CLI.Infrastructure;

/// <summary>
/// Generates Razor views with HTMX and Bulma CSS for CRUD operations.
/// </summary>
public static class ViewGenerator
{
    /// <summary>
    /// Generates Index.cshtml (main page with layout).
    /// </summary>
    public static string GenerateIndexView(EntityGenerationOptions options)
    {
        var sb = new StringBuilder();

        sb.AppendLine("@{");
        sb.AppendLine($"    ViewData[\"Title\"] = \"{options.EntityName}s\";");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("<div class=\"container\">");
        sb.AppendLine("    <div class=\"section\">");
        sb.AppendLine("        <h1 class=\"title\">");
        sb.AppendLine($"            <i class=\"fas fa-box\"></i> {options.EntityName}s");
        sb.AppendLine("        </h1>");
        sb.AppendLine();

        if (options.HasSearch || options.HasFilters)
        {
            sb.AppendLine("        <!-- Search and Filters -->");
            sb.AppendLine("        <div class=\"box\">");
            
            if (options.HasSearch)
            {
                sb.AppendLine("            <div class=\"field\">");
                sb.AppendLine("                <label class=\"label\">Search</label>");
                sb.AppendLine("                <div class=\"control has-icons-left\">");
                sb.AppendLine("                    <input type=\"text\" ");
                sb.AppendLine("                           class=\"input\" ");
                sb.AppendLine("                           name=\"searchQuery\"");
                sb.AppendLine("                           placeholder=\"Search...\"");
                sb.AppendLine($"                           hx-get=\"/{options.EntityName}/List\"");
                sb.AppendLine("                           hx-trigger=\"keyup changed delay:500ms\"");
                sb.AppendLine("                           hx-target=\"#list-container\"");
                sb.AppendLine("                           hx-include=\"[name='searchQuery'],[name^='filter']\">");
                sb.AppendLine("                    <span class=\"icon is-left\">");
                sb.AppendLine("                        <i class=\"fas fa-search\"></i>");
                sb.AppendLine("                    </span>");
                sb.AppendLine("                </div>");
                sb.AppendLine("            </div>");
            }

            if (options.HasFilters)
            {
                sb.AppendLine("            <div class=\"field is-grouped\">");
                foreach (var filterProp in options.FilterableProperties.Take(3)) // Show first 3 filters
                {
                    if (!filterProp.EndsWith("Range", StringComparison.OrdinalIgnoreCase))
                    {
                        var prop = options.Properties.FirstOrDefault(p => 
                            p.Name.Equals(filterProp, StringComparison.OrdinalIgnoreCase));
                        
                        if (prop != null)
                        {
                            var propName = char.ToUpper(prop.Name[0]) + prop.Name[1..];
                            sb.AppendLine("                <div class=\"control\">");
                            sb.AppendLine($"                    <label class=\"label\">{propName}</label>");
                            sb.AppendLine($"                    <input type=\"text\" ");
                            sb.AppendLine("                           class=\"input\"");
                            sb.AppendLine($"                           name=\"filter{propName}\"");
                            sb.AppendLine($"                           hx-get=\"/{options.EntityName}/List\"");
                            sb.AppendLine("                           hx-trigger=\"change\"");
                            sb.AppendLine("                           hx-target=\"#list-container\"");
                            sb.AppendLine("                           hx-include=\"[name='searchQuery'],[name^='filter']\">");
                            sb.AppendLine("                </div>");
                        }
                    }
                }
                sb.AppendLine("            </div>");
            }

            sb.AppendLine("        </div>");
            sb.AppendLine();
        }

        sb.AppendLine("        <!-- Actions -->");
        sb.AppendLine("        <div class=\"level\">");
        sb.AppendLine("            <div class=\"level-left\"></div>");
        sb.AppendLine("            <div class=\"level-right\">");
        sb.AppendLine("                <div class=\"level-item\">");
        sb.AppendLine("                    <button class=\"button is-primary\"");
        sb.AppendLine($"                            hx-get=\"/{options.EntityName}/Create\"");
        sb.AppendLine("                            hx-target=\"#modal-container\">");
        sb.AppendLine("                        <span class=\"icon\">");
        sb.AppendLine("                            <i class=\"fas fa-plus\"></i>");
        sb.AppendLine("                        </span>");
        sb.AppendLine($"                        <span>New {options.EntityName}</span>");
        sb.AppendLine("                    </button>");
        sb.AppendLine("                </div>");
        sb.AppendLine("            </div>");
        sb.AppendLine("        </div>");
        sb.AppendLine();
        sb.AppendLine("        <!-- List Container -->");
        sb.AppendLine("        <div id=\"list-container\"");
        sb.AppendLine($"             hx-get=\"/{options.EntityName}/List\"");
        sb.AppendLine($"             hx-trigger=\"load, {options.EntityName.ToLower()}-created from:body, {options.EntityName.ToLower()}-updated from:body, {options.EntityName.ToLower()}-deleted from:body\"");
        sb.AppendLine("             hx-include=\"[name='searchQuery'],[name^='filter']\">");
        sb.AppendLine("            <div class=\"has-text-centered\">");
        sb.AppendLine("                <span class=\"icon is-large\">");
        sb.AppendLine("                    <i class=\"fas fa-spinner fa-pulse\"></i>");
        sb.AppendLine("                </span>");
        sb.AppendLine("            </div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("    </div>");
        sb.AppendLine("</div>");
        sb.AppendLine();
        sb.AppendLine("<!-- Modal Container -->");
        sb.AppendLine("<div id=\"modal-container\"></div>");

        return sb.ToString();
    }

    /// <summary>
    /// Generates _List.cshtml (table partial with HTMX).
    /// </summary>
    public static string GenerateListView(EntityGenerationOptions options)
    {
        var sb = new StringBuilder();

        // Model declaration
        if (options.HasPagination)
        {
            sb.AppendLine($"@model Paged{options.EntityName}ResultDto");
        }
        else
        {
            sb.AppendLine($"@model List<{options.EntityName}Dto>");
        }
        sb.AppendLine();

        // Table
        sb.AppendLine("<div class=\"table-container\">");
        sb.AppendLine("    <table class=\"table is-fullwidth is-striped is-hoverable\">");
        sb.AppendLine("        <thead>");
        sb.AppendLine("            <tr>");

        // Table headers with sorting
        var displayProps = options.Properties.Where(p => !p.IsCollection).Take(5).ToList();
        foreach (var prop in displayProps)
        {
            var propName = char.ToUpper(prop.Name[0]) + prop.Name[1..];
            
            if (options.HasSorting && options.SortableProperties.Contains(prop.Name, StringComparer.OrdinalIgnoreCase))
            {
                sb.AppendLine($"                <th>");
                sb.AppendLine($"                    <a hx-get=\"/{options.EntityName}/List?sortBy={prop.Name.ToLower()}\"");
                sb.AppendLine("                       hx-target=\"#list-container\"");
                sb.AppendLine("                       hx-include=\"[name='searchQuery'],[name^='filter']\">");
                sb.AppendLine($"                        {propName}");
                sb.AppendLine("                        <span class=\"icon is-small\">");
                sb.AppendLine("                            <i class=\"fas fa-sort\"></i>");
                sb.AppendLine("                        </span>");
                sb.AppendLine("                    </a>");
                sb.AppendLine("                </th>");
            }
            else
            {
                sb.AppendLine($"                <th>{propName}</th>");
            }
        }

        sb.AppendLine("                <th class=\"has-text-right\">Actions</th>");
        sb.AppendLine("            </tr>");
        sb.AppendLine("        </thead>");
        sb.AppendLine("        <tbody>");

        // Check for empty list
        if (options.HasPagination)
        {
            sb.AppendLine("            @if (!Model.Items.Any())");
        }
        else
        {
            sb.AppendLine("            @if (!Model.Any())");
        }
        
        sb.AppendLine("            {");
        sb.AppendLine("                <tr>");
        sb.AppendLine($"                    <td colspan=\"{displayProps.Count + 1}\" class=\"has-text-centered\">");
        sb.AppendLine($"                        <p class=\"has-text-grey\">No {options.EntityName.ToLower()}s found.</p>");
        sb.AppendLine("                    </td>");
        sb.AppendLine("                </tr>");
        sb.AppendLine("            }");

        // Iterate items
        if (options.HasPagination)
        {
            sb.AppendLine("            @foreach (var item in Model.Items)");
        }
        else
        {
            sb.AppendLine("            @foreach (var item in Model)");
        }
        
        sb.AppendLine("            {");
        sb.AppendLine("                <tr id=\"row-@item.Id\">");

        // Display properties
        foreach (var prop in displayProps)
        {
            var propName = char.ToUpper(prop.Name[0]) + prop.Name[1..];
            
            if (prop.Type == "DateTime" || prop.Type == "DateTime?")
            {
                sb.AppendLine($"                    <td>@item.{propName}?.ToString(\"g\")</td>");
            }
            else if (prop.Type == "decimal" || prop.Type == "decimal?")
            {
                sb.AppendLine($"                    <td>@item.{propName}?.ToString(\"C\")</td>");
            }
            else if (prop.Type == "bool" || prop.Type == "bool?")
            {
                sb.AppendLine($"                    <td>");
                sb.AppendLine($"                        @if (item.{propName} == true)");
                sb.AppendLine("                        {");
                sb.AppendLine("                            <span class=\"icon has-text-success\">");
                sb.AppendLine("                                <i class=\"fas fa-check\"></i>");
                sb.AppendLine("                            </span>");
                sb.AppendLine("                        }");
                sb.AppendLine("                        else");
                sb.AppendLine("                        {");
                sb.AppendLine("                            <span class=\"icon has-text-grey-light\">");
                sb.AppendLine("                                <i class=\"fas fa-times\"></i>");
                sb.AppendLine("                            </span>");
                sb.AppendLine("                        }");
                sb.AppendLine("                    </td>");
            }
            else
            {
                sb.AppendLine($"                    <td>@item.{propName}</td>");
            }
        }

        // Actions
        sb.AppendLine("                    <td class=\"has-text-right\">");
        sb.AppendLine("                        <div class=\"buttons is-right\">");
        sb.AppendLine("                            <button class=\"button is-small is-info\"");
        sb.AppendLine($"                                    hx-get=\"/{options.EntityName}/Edit/@item.Id\"");
        sb.AppendLine("                                    hx-target=\"#modal-container\">");
        sb.AppendLine("                                <span class=\"icon is-small\">");
        sb.AppendLine("                                    <i class=\"fas fa-edit\"></i>");
        sb.AppendLine("                                </span>");
        sb.AppendLine("                            </button>");
        sb.AppendLine("                            <button class=\"button is-small is-danger\"");
        sb.AppendLine($"                                    hx-delete=\"/{options.EntityName}/Delete/@item.Id\"");
        sb.AppendLine("                                    hx-target=\"#row-@item.Id\"");
        sb.AppendLine("                                    hx-confirm=\"Are you sure you want to delete this item?\">");
        sb.AppendLine("                                <span class=\"icon is-small\">");
        sb.AppendLine("                                    <i class=\"fas fa-trash\"></i>");
        sb.AppendLine("                                </span>");
        sb.AppendLine("                            </button>");
        sb.AppendLine("                        </div>");
        sb.AppendLine("                    </td>");
        sb.AppendLine("                </tr>");
        sb.AppendLine("            }");
        sb.AppendLine("        </tbody>");
        sb.AppendLine("    </table>");
        sb.AppendLine("</div>");

        // Pagination
        if (options.HasPagination)
        {
            sb.AppendLine();
            sb.AppendLine("<nav class=\"pagination is-centered\" role=\"navigation\">");
            sb.AppendLine("    <a class=\"pagination-previous\"");
            sb.AppendLine("       @(Model.HasPreviousPage ? \"\" : \"disabled\")");
            sb.AppendLine($"       hx-get=\"/{options.EntityName}/List?pageNumber=@(Model.PageNumber - 1)\"");
            sb.AppendLine("       hx-target=\"#list-container\"");
            sb.AppendLine("       hx-include=\"[name='searchQuery'],[name^='filter']\">");
            sb.AppendLine("        Previous");
            sb.AppendLine("    </a>");
            sb.AppendLine("    <a class=\"pagination-next\"");
            sb.AppendLine("       @(Model.HasNextPage ? \"\" : \"disabled\")");
            sb.AppendLine($"       hx-get=\"/{options.EntityName}/List?pageNumber=@(Model.PageNumber + 1)\"");
            sb.AppendLine("       hx-target=\"#list-container\"");
            sb.AppendLine("       hx-include=\"[name='searchQuery'],[name^='filter']\">");
            sb.AppendLine("        Next");
            sb.AppendLine("    </a>");
            sb.AppendLine("    <ul class=\"pagination-list\">");
            sb.AppendLine("        <li><span class=\"pagination-link is-current\">Page @Model.PageNumber of @Model.TotalPages</span></li>");
            sb.AppendLine("    </ul>");
            sb.AppendLine("</nav>");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generates _Form.cshtml (create/edit modal with HTMX).
    /// </summary>
    public static string GenerateFormView(EntityGenerationOptions options)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"@model dynamic");
        sb.AppendLine();
        sb.AppendLine("@{");
        sb.AppendLine($"    var isEdit = Model.GetType().Name.StartsWith(\"Update\");");
        sb.AppendLine($"    var action = isEdit ? \"Edit\" : \"Create\";");
        sb.AppendLine($"    var title = isEdit ? \"Edit {options.EntityName}\" : \"New {options.EntityName}\";");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("<div class=\"modal is-active\">");
        sb.AppendLine("    <div class=\"modal-background\" onclick=\"this.closest('.modal').remove()\"></div>");
        sb.AppendLine("    <div class=\"modal-card\">");
        sb.AppendLine("        <header class=\"modal-card-head\">");
        sb.AppendLine("            <p class=\"modal-card-title\">@title</p>");
        sb.AppendLine("            <button class=\"delete\" aria-label=\"close\" onclick=\"this.closest('.modal').remove()\"></button>");
        sb.AppendLine("        </header>");
        sb.AppendLine("        <form hx-post=\"/@($\"/{options.EntityName}/{action}\")\"");
        sb.AppendLine("              hx-target=\"#modal-container\"");
        sb.AppendLine("              hx-swap=\"outerHTML\">");
        sb.AppendLine("            <section class=\"modal-card-body\">");
        sb.AppendLine("                @if (isEdit)");
        sb.AppendLine("                {");
        sb.AppendLine("                    <input type=\"hidden\" name=\"Id\" value=\"@Model.Id\" />");
        sb.AppendLine("                }");
        sb.AppendLine();

        // Generate form fields
        var formProps = options.Properties.Where(p => !p.IsCollection).ToList();
        foreach (var prop in formProps)
        {
            var propName = char.ToUpper(prop.Name[0]) + prop.Name[1..];
            
            sb.AppendLine("                <div class=\"field\">");
            sb.AppendLine($"                    <label class=\"label\">{propName}</label>");
            sb.AppendLine("                    <div class=\"control\">");

            if (prop.Type == "string")
            {
                if (prop.MaxLength.HasValue && prop.MaxLength > 500)
                {
                    // Textarea for long strings
                    sb.AppendLine($"                        <textarea class=\"textarea\"");
                    sb.AppendLine($"                                  name=\"{propName}\"");
                    sb.AppendLine($"                                  {(prop.IsRequired ? "required" : "")}");
                    sb.AppendLine($"                                  maxlength=\"{prop.MaxLength}\">@Model.{propName}</textarea>");
                }
                else
                {
                    // Input for normal strings
                    sb.AppendLine($"                        <input class=\"input\"");
                    sb.AppendLine("                               type=\"text\"");
                    sb.AppendLine($"                               name=\"{propName}\"");
                    sb.AppendLine($"                               value=\"@Model.{propName}\"");
                    sb.AppendLine($"                               {(prop.IsRequired ? "required" : "")}");
                    if (prop.MaxLength.HasValue)
                    {
                        sb.AppendLine($"                               maxlength=\"{prop.MaxLength}\"");
                    }
                    sb.AppendLine("                               />");
                }
            }
            else if (prop.Type == "int" || prop.Type == "int?")
            {
                sb.AppendLine($"                        <input class=\"input\"");
                sb.AppendLine("                               type=\"number\"");
                sb.AppendLine($"                               name=\"{propName}\"");
                sb.AppendLine($"                               value=\"@Model.{propName}\"");
                sb.AppendLine($"                               {(prop.IsRequired ? "required" : "")} />");
            }
            else if (prop.Type == "decimal" || prop.Type == "decimal?")
            {
                sb.AppendLine($"                        <input class=\"input\"");
                sb.AppendLine("                               type=\"number\"");
                sb.AppendLine("                               step=\"0.01\"");
                sb.AppendLine($"                               name=\"{propName}\"");
                sb.AppendLine($"                               value=\"@Model.{propName}\"");
                sb.AppendLine($"                               {(prop.IsRequired ? "required" : "")} />");
            }
            else if (prop.Type == "bool" || prop.Type == "bool?")
            {
                sb.AppendLine("                        <label class=\"checkbox\">");
                sb.AppendLine($"                            <input type=\"checkbox\"");
                sb.AppendLine($"                                   name=\"{propName}\"");
                sb.AppendLine($"                                   @(Model.{propName} == true ? \"checked\" : \"\") />");
                sb.AppendLine($"                            {propName}");
                sb.AppendLine("                        </label>");
            }
            else if (prop.Type == "DateTime" || prop.Type == "DateTime?")
            {
                sb.AppendLine($"                        <input class=\"input\"");
                sb.AppendLine("                               type=\"datetime-local\"");
                sb.AppendLine($"                               name=\"{propName}\"");
                sb.AppendLine($"                               value=\"@Model.{propName}?.ToString(\"yyyy-MM-ddTHH:mm\")\"");
                sb.AppendLine($"                               {(prop.IsRequired ? "required" : "")} />");
            }
            else
            {
                // Default text input
                sb.AppendLine($"                        <input class=\"input\"");
                sb.AppendLine("                               type=\"text\"");
                sb.AppendLine($"                               name=\"{propName}\"");
                sb.AppendLine($"                               value=\"@Model.{propName}\"");
                sb.AppendLine($"                               {(prop.IsRequired ? "required" : "")} />");
            }

            sb.AppendLine("                    </div>");
            
            // Validation message
            sb.AppendLine("                    <p class=\"help is-danger\">");
            sb.AppendLine($"                        <span asp-validation-for=\"{propName}\"></span>");
            sb.AppendLine("                    </p>");
            
            sb.AppendLine("                </div>");
            sb.AppendLine();
        }

        sb.AppendLine("            </section>");
        sb.AppendLine("            <footer class=\"modal-card-foot\">");
        sb.AppendLine("                <button type=\"submit\" class=\"button is-success\">Save</button>");
        sb.AppendLine("                <button type=\"button\" class=\"button\" onclick=\"this.closest('.modal').remove()\">Cancel</button>");
        sb.AppendLine("            </footer>");
        sb.AppendLine("        </form>");
        sb.AppendLine("    </div>");
        sb.AppendLine("</div>");
        sb.AppendLine();
        sb.AppendLine("<script>");
        sb.AppendLine("    // Close modal on successful save");
        sb.AppendLine("    document.body.addEventListener('htmx:afterSwap', function(evt) {");
        sb.AppendLine("        if (evt.detail.successful && evt.detail.xhr.status === 200) {");
        sb.AppendLine("            const modal = document.querySelector('.modal');");
        sb.AppendLine("            if (modal) modal.remove();");
        sb.AppendLine("        }");
        sb.AppendLine("    });");
        sb.AppendLine("</script>");

        return sb.ToString();
    }
}
