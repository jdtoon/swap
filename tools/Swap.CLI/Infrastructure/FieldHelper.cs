namespace Swap.CLI.Infrastructure;

public class FieldDefinition
{
    public required string Name { get; set; }
    public required string Type { get; set; }
    public bool IsNullable { get; set; }
    public bool IsRequired { get; set; }
    
    /// <summary>
    /// Whether this field should be sortable in list views. Default: true
    /// </summary>
    public bool IsSortable { get; set; } = true;
    
    /// <summary>
    /// Whether this field should have a filter control. Default: false
    /// Only applies to bool fields currently.
    /// </summary>
    public bool IsFilterable { get; set; } = false;
}

public static class FieldHelper
{
    /// <summary>
    /// Parse field specifications like "Name:string:s Title:string?:ns,f Age:int:sortable,filterable"
    /// Supports flags: :sortable/:s (sortable), :nosort/:ns (not sortable), :filterable/:f (filterable)
    /// Defaults: sortable=true, filterable=false
    /// </summary>
    public static List<FieldDefinition> ParseFields(string fieldsSpec)
    {
        var fields = new List<FieldDefinition>();
        
        if (string.IsNullOrWhiteSpace(fieldsSpec))
        {
            return fields;
        }
        
        // Split by space only to handle field specs
        // Don't split by comma since commas are used within flag specifications
        var fieldSpecs = fieldsSpec.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var spec in fieldSpecs)
        {
            // Split by colon: FieldName:Type:Flags
            var parts = spec.Split(':');
            if (parts.Length < 2)
            {
                throw new ArgumentException($"Invalid field specification: {spec}. Expected format: 'FieldName:type[:flags]'");
            }
            
            var fieldName = parts[0].Trim();
            var typeSpec = parts[1].Trim();
            
            // Parse flags (everything after the type)
            var flags = parts.Length > 2 
                ? string.Join(":", parts.Skip(2)).ToLower().Split(',', StringSplitOptions.RemoveEmptyEntries).Select(f => f.Trim()).ToList()
                : new List<string>();
            
            // Validate field name
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                throw new ArgumentException($"Field name cannot be empty in: {spec}");
            }
            
            // Auto-capitalize field name
            if (!char.IsUpper(fieldName[0]))
            {
                fieldName = char.ToUpper(fieldName[0]) + fieldName.Substring(1);
            }
            
            // Check for nullable marker
            bool isNullable = typeSpec.EndsWith('?');
            if (isNullable)
            {
                typeSpec = typeSpec.TrimEnd('?');
            }
            
            // Map type names
            var mappedType = MapType(typeSpec);
            
            // Parse flags
            bool isSortable = true; // Default
            bool isFilterable = false; // Default
            
            foreach (var flag in flags)
            {
                switch (flag)
                {
                    case "sortable":
                    case "s":
                        isSortable = true;
                        break;
                    case "nosort":
                    case "ns":
                        isSortable = false;
                        break;
                    case "filterable":
                    case "f":
                        isFilterable = true;
                        break;
                    default:
                        throw new ArgumentException($"Unknown flag '{flag}' in field specification: {spec}. Valid flags: sortable/s, nosort/ns, filterable/f");
                }
            }
            
            fields.Add(new FieldDefinition
            {
                Name = fieldName,
                Type = mappedType,
                IsNullable = isNullable,
                IsRequired = mappedType == "string" ? !isNullable : !isNullable,
                IsSortable = isSortable,
                IsFilterable = isFilterable
            });
        }
        
        return fields;
    }
    
    /// <summary>
    /// Generate initialization code for Create action (sets default values like DateTime.Now)
    /// </summary>
    public static string GenerateDefaultInitialization(List<FieldDefinition> fields)
    {
        var initializations = new List<string>();
        
        foreach (var field in fields)
        {
            if (field.Type == "DateTime" && !field.IsNullable)
            {
                initializations.Add($"            {field.Name} = DateTime.Now");
            }
        }
        
        if (!initializations.Any())
        {
            return "";
        }
        
        return string.Join(",\n", initializations);
    }
    
    private static string MapType(string typeSpec)
    {
        return typeSpec.ToLower() switch
        {
            "string" => "string",
            "int" => "int",
            "long" => "long",
            "short" => "short",
            "byte" => "byte",
            "bool" => "bool",
            "boolean" => "bool",
            "float" => "float",
            "double" => "double",
            "decimal" => "decimal",
            "datetime" => "DateTime",
            "date" => "DateTime",
            "guid" => "Guid",
            _ => throw new ArgumentException($"Unsupported field type: {typeSpec}")
        };
    }
    
    /// <summary>
    /// Generate form field HTML for Create/Edit forms
    /// </summary>
    public static string GenerateFormField(FieldDefinition field)
    {
        var inputType = GetHtmlInputType(field.Type);
        var required = field.IsRequired ? "required" : "";
        
        return field.Type switch
        {
            "bool" => $@"<div class=""form-control"">
                <label class=""label cursor-pointer"">
                    <span class=""label-text"">{field.Name}</span>
                    <input type=""checkbox"" name=""{field.Name}"" value=""true"" class=""checkbox checkbox-primary"" @(Model.{field.Name} ? ""checked"" : """") />
                </label>
                <input type=""hidden"" name=""{field.Name}"" value=""false"" />
            </div>",
            
            "DateTime" => $@"<div class=""form-control"">
                <label class=""label"">
                    <span class=""label-text"">{field.Name}</span>
                </label>
                <input type=""datetime-local"" 
                       name=""{field.Name}"" 
                       placeholder=""{field.Name}""
                       value=""@Model.{field.Name}.ToString(""yyyy-MM-ddTHH:mm"")""
                       class=""input input-bordered"" 
                       {required} />
                <span asp-validation-for=""{field.Name}"" class=""text-error text-sm""></span>
            </div>",
            
            "decimal" or "float" or "double" => $@"<div class=""form-control"">
                <label class=""label"">
                    <span class=""label-text"">{field.Name}</span>
                </label>
                <input type=""{inputType}"" 
                       name=""{field.Name}"" 
                       placeholder=""{field.Name}""
                       value=""@Model.{field.Name}""
                       step=""any""
                       class=""input input-bordered"" 
                       {required} />
                <span asp-validation-for=""{field.Name}"" class=""text-error text-sm""></span>
            </div>",
            
            _ => $@"<div class=""form-control"">
                <label class=""label"">
                    <span class=""label-text"">{field.Name}</span>
                </label>
                <input type=""{inputType}"" 
                       name=""{field.Name}"" 
                       placeholder=""{field.Name}""
                       value=""@Model.{field.Name}""
                       class=""input input-bordered"" 
                       {required} />
                <span asp-validation-for=""{field.Name}"" class=""text-error text-sm""></span>
            </div>"
        };
    }
    
    /// <summary>
    /// Generate table header for list view
    /// Sortable fields get clickable button with sort indicators
    /// Non-sortable fields get plain text header
    /// </summary>
    public static string GenerateTableHeader(FieldDefinition field, string entityNameLower)
    {
        var fieldNameLower = field.Name.ToLower();
        
        if (!field.IsSortable)
        {
            // Non-sortable: just plain text
            return $@"<th>{field.Name}</th>";
        }
        
        // Sortable: button with HTMX and sort indicators
        return $@"<th>
                        <button class=""flex items-center gap-1 hover:text-primary""
                                hx-get=""@Url.Action(""Index"")""
                                hx-target=""#{entityNameLower}-list""
                                hx-swap=""innerHTML""
                                hx-include=""[name='searchTerm'], [name='pageSize']""
                                hx-vals='{{""sortBy"": ""{fieldNameLower}"", ""sortOrder"": ""@(Model.SortBy?.ToLower() == ""{fieldNameLower}"" && Model.SortOrder == ""asc"" ? ""desc"" : ""asc"")""}}'
                                type=""button"">
                            {field.Name}
                            @if (Model.SortBy?.ToLower() == ""{fieldNameLower}"")
                            {{
                                @if (Model.SortOrder == ""desc"")
                                {{
                                    <span>↓</span>
                                }}
                                else
                                {{
                                    <span>↑</span>
                                }}
                            }}
                        </button>
                    </th>";
    }
    
    /// <summary>
    /// Generate table cell for list view
    /// </summary>
    public static string GenerateTableCell(FieldDefinition field)
    {
        return field.Type switch
        {
            "bool" => $@"<td>
                            @if (item.{field.Name})
                            {{
                                <span class=""badge badge-success"">Yes</span>
                            }}
                            else
                            {{
                                <span class=""badge badge-ghost"">No</span>
                            }}
                        </td>",
            
            "DateTime" => $"<td>@item.{field.Name}.ToString(\"yyyy-MM-dd\")</td>",
            
            _ => $"<td>@item.{field.Name}</td>"
        };
    }
    
    /// <summary>
    /// Generate details field for Details modal
    /// </summary>
    public static string GenerateDetailsField(FieldDefinition field)
    {
        return field.Type switch
        {
            "bool" => $@"<div>
                <span class=""font-semibold"">{field.Name}:</span>
                @if (Model.{field.Name})
                {{
                    <span class=""badge badge-success"">Yes</span>
                }}
                else
                {{
                    <span class=""badge badge-ghost"">No</span>
                }}
            </div>",
            
            "DateTime" => $@"<div>
                <span class=""font-semibold"">{field.Name}:</span>
                <span>@Model.{field.Name}.ToString(""yyyy-MM-dd HH:mm"")</span>
            </div>",
            
            _ => $@"<div>
                <span class=""font-semibold"">{field.Name}:</span>
                <span>@Model.{field.Name}</span>
            </div>"
        };
    }
    
    /// <summary>
    /// Generate search logic for string fields
    /// </summary>
    public static string GenerateSearchLogic(List<FieldDefinition> fields)
    {
        var stringFields = fields.Where(f => f.Type == "string").ToList();
        
        if (!stringFields.Any())
        {
            return "// No searchable fields";
        }
        
        // Use ToLower() for case-insensitive search - works with all EF providers
        var conditions = stringFields.Select(f => 
            $"x.{f.Name}{(f.IsNullable ? "!" : "")}.ToLower().Contains(searchTerm.ToLower())");
        
        return $"query = query.Where(x => {string.Join(" || ", conditions)});";
    }
    
    /// <summary>
    /// Generate sort cases for ApplySorting method
    /// Only includes fields where IsSortable = true
    /// </summary>
    public static string GenerateSortCases(List<FieldDefinition> fields)
    {
        var sortableFields = fields.Where(f => f.IsSortable).ToList();
        
        if (!sortableFields.Any())
        {
            return "// No sortable fields";
        }
        
        var cases = sortableFields.Select(f => 
            $@"""{f.Name.ToLower()}"" => isDescending ? query.OrderByDescending(x => x.{f.Name}) : query.OrderBy(x => x.{f.Name}),"
        );
        
        return string.Join("\n            ", cases);
    }
    
    private static string GetHtmlInputType(string csharpType)
    {
        return csharpType switch
        {
            "string" => "text",
            "int" or "long" or "short" or "byte" => "number",
            "float" or "double" or "decimal" => "number",
            "bool" => "checkbox",
            "DateTime" => "datetime-local",
            "Guid" => "text",
            _ => "text"
        };
    }
    
    /// <summary>
    /// Generate filter parameters for controller Index action
    /// Only includes bool fields where IsFilterable = true
    /// </summary>
    public static string GenerateFilterParameters(List<FieldDefinition> fields)
    {
        var filterableFields = fields.Where(f => f.Type == "bool" && f.IsFilterable).ToList();
        if (!filterableFields.Any())
            return string.Empty;
            
        var parameters = filterableFields.Select(f => 
            $"bool? {char.ToLower(f.Name[0]) + f.Name.Substring(1)} = null");
        return ", " + string.Join(", ", parameters);
    }
    
    /// <summary>
    /// Generate filter logic for ApplyFilters method
    /// Only includes bool fields where IsFilterable = true
    /// </summary>
    public static string GenerateFilterCases(List<FieldDefinition> fields)
    {
        var filterableFields = fields.Where(f => f.Type == "bool" && f.IsFilterable).ToList();
        if (!filterableFields.Any())
            return "// No filterable fields";
            
        var cases = filterableFields.Select(f =>
        {
            var paramName = char.ToLower(f.Name[0]) + f.Name.Substring(1);
            return $@"if ({paramName}.HasValue)
        {{
            query = query.Where(x => x.{f.Name} == {paramName}.Value);
        }}";
        });
        
        return string.Join("\n        ", cases);
    }
    
    /// <summary>
    /// Generate filter controls UI for bool fields
    /// Only includes bool fields where IsFilterable = true
    /// </summary>
    public static string GenerateFilterControls(List<FieldDefinition> fields, string entityNameLower)
    {
        var filterableFields = fields.Where(f => f.Type == "bool" && f.IsFilterable).ToList();
        if (!filterableFields.Any())
            return string.Empty;
            
        var controls = filterableFields.Select(f =>
        {
            var paramName = char.ToLower(f.Name[0]) + f.Name.Substring(1);
            return $@"<div class=""form-control"">
                        <label class=""label"">
                            <span class=""label-text"">{f.Name}</span>
                        </label>
                        <select name=""{paramName}"" 
                                class=""select select-bordered w-full""
                                hx-get=""@Url.Action(""Index"")""
                                hx-target=""#{entityNameLower}-list""
                                hx-swap=""innerHTML""
                                hx-include=""[name='searchTerm'], [name='pageSize'], [name='sortBy'], [name='sortOrder'], [name='{paramName}']""
                                hx-trigger=""change"">
                            <option value="""">All</option>
                            <option value=""true"">Yes</option>
                            <option value=""false"">No</option>
                        </select>
                    </div>";
        });
        
        return string.Join("\n                    ", controls);
    }
    
    /// <summary>
    /// Generate parameter values for passing to ApplyFilters method
    /// Only includes bool fields where IsFilterable = true
    /// </summary>
    public static string GenerateFilterParameterValues(List<FieldDefinition> fields)
    {
        var filterableFields = fields.Where(f => f.Type == "bool" && f.IsFilterable).ToList();
        if (!filterableFields.Any())
            return string.Empty;
            
        var parameters = filterableFields.Select(f => 
            char.ToLower(f.Name[0]) + f.Name.Substring(1));
        return ", " + string.Join(", ", parameters);
    }
    
    /// <summary>
    /// Generate filter dictionary entries for view model
    /// Only includes bool fields where IsFilterable = true
    /// </summary>
    public static string GenerateFilterDictionary(List<FieldDefinition> fields)
    {
        var filterableFields = fields.Where(f => f.Type == "bool" && f.IsFilterable).ToList();
        if (!filterableFields.Any())
            return string.Empty;
            
        var entries = filterableFields.Select(f =>
        {
            var paramName = char.ToLower(f.Name[0]) + f.Name.Substring(1);
            return $@"{{ ""{paramName}"", {paramName}?.ToString().ToLower() }}";
        });
        
        return string.Join(",\n                ", entries);
    }
    
    /// <summary>
    /// Generate hx-include additions for filter fields
    /// Only includes bool fields where IsFilterable = true
    /// </summary>
    public static string GenerateFilterIncludes(List<FieldDefinition> fields)
    {
        var filterableFields = fields.Where(f => f.Type == "bool" && f.IsFilterable).ToList();
        if (!filterableFields.Any())
            return string.Empty;
            
        var includes = filterableFields.Select(f =>
        {
            var paramName = char.ToLower(f.Name[0]) + f.Name.Substring(1);
            return $"[name='{paramName}']";
        });
        
        return ", " + string.Join(", ", includes);
    }
    
    /// <summary>
    /// Generate complete filter section HTML
    /// </summary>
    public static string GenerateFilterSection(List<FieldDefinition> fields, string entityNameLower)
    {
        var controls = GenerateFilterControls(fields, entityNameLower);
        if (string.IsNullOrEmpty(controls))
            return string.Empty;
            
        return $@"<div class=""mb-4"">
                <div class=""grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4"">
                    {controls}
                </div>
            </div>";
    }
    
    /// <summary>
    /// Generate checkbox column header for bulk selection (Select All)
    /// </summary>
    public static string GenerateBulkSelectHeader()
    {
        return @"<th>
                        <input type=""checkbox"" 
                               id=""select-all"" 
                               class=""checkbox checkbox-sm""
                               onclick=""toggleSelectAll(this)"" />
                    </th>";
    }
    
    /// <summary>
    /// Generate checkbox cell for bulk selection
    /// </summary>
    public static string GenerateBulkSelectCell(string entityNameLower)
    {
        return $@"<td>
                            <input type=""checkbox"" 
                                   class=""checkbox checkbox-sm {entityNameLower}-checkbox"" 
                                   value=""@item.Id""
                                   onclick=""updateBulkActions()"" />
                        </td>";
    }
    
    /// <summary>
    /// Generate JavaScript for bulk selection management
    /// </summary>
    public static string GenerateBulkSelectionScript(string entityNameLower)
    {
        return $@"<script>
    function toggleSelectAll(checkbox) {{
        const checkboxes = document.querySelectorAll('.{entityNameLower}-checkbox');
        checkboxes.forEach(cb => cb.checked = checkbox.checked);
        updateBulkActions();
    }}
    
    function updateBulkActions() {{
        const checkboxes = document.querySelectorAll('.{entityNameLower}-checkbox:checked');
        const bulkActions = document.getElementById('bulk-actions');
        const selectedCount = document.getElementById('selected-count');
        
        if (checkboxes.length > 0) {{
            bulkActions.classList.remove('hidden');
            selectedCount.textContent = checkboxes.length;
        }} else {{
            bulkActions.classList.add('hidden');
        }}
        
        // Update select-all checkbox state
        const allCheckboxes = document.querySelectorAll('.{entityNameLower}-checkbox');
        const selectAllCheckbox = document.getElementById('select-all');
        if (selectAllCheckbox) {{
            selectAllCheckbox.checked = allCheckboxes.length > 0 && checkboxes.length === allCheckboxes.length;
            selectAllCheckbox.indeterminate = checkboxes.length > 0 && checkboxes.length < allCheckboxes.length;
        }}
    }}
    
    function getSelectedIds() {{
        const checkboxes = document.querySelectorAll('.{entityNameLower}-checkbox:checked');
        return Array.from(checkboxes).map(cb => parseInt(cb.value));
    }}
    
    function clearSelection() {{
        const checkboxes = document.querySelectorAll('.{entityNameLower}-checkbox');
        checkboxes.forEach(cb => cb.checked = false);
        document.getElementById('select-all').checked = false;
        updateBulkActions();
    }}
</script>";
    }
    
    /// <summary>
    /// Generate bulk actions bar UI
    /// </summary>
    public static string GenerateBulkActionsBar(string entityName, string entityNameLower)
    {
        return $@"<!-- Bulk Actions Bar -->
            <div id=""bulk-actions"" class=""hidden alert alert-info mb-4"">
                <div class=""flex justify-between items-center w-full"">
                    <span><strong id=""selected-count"">0</strong> item(s) selected</span>
                    <div class=""flex gap-2"">
                        <button onclick=""confirmBulkDelete()"" class=""btn btn-sm btn-error"">
                            <svg xmlns=""http://www.w3.org/2000/svg"" class=""h-4 w-4"" viewBox=""0 0 20 20"" fill=""currentColor"">
                                <path fill-rule=""evenodd"" d=""M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z"" clip-rule=""evenodd"" />
                            </svg>
                            Delete Selected
                        </button>
                        <button onclick=""clearSelection()"" class=""btn btn-sm btn-ghost"">
                            Clear Selection
                        </button>
                    </div>
                </div>
            </div>";
    }
    
    /// <summary>
    /// Generate bulk delete confirmation and HTMX script
    /// </summary>
    public static string GenerateBulkDeleteScript(string entityName, string entityNameLower)
    {
        return $@"<script>
    function confirmBulkDelete() {{
        const ids = getSelectedIds();
        if (ids.length === 0) return;
        
        if (confirm(`Are you sure you want to delete ${{ids.length}} {entityNameLower}(s)? This action cannot be undone.`)) {{
            bulkDelete(ids);
        }}
    }}
    
    function bulkDelete(ids) {{
        fetch('@Url.Action(""BulkDelete"")', {{
            method: 'POST',
            headers: {{
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name=""__RequestVerificationToken""]')?.value || ''
            }},
            body: JSON.stringify(ids)
        }})
        .then(response => {{
            if (response.ok) {{
                // Trigger list refresh
                htmx.trigger('#{entityNameLower}-list', 'refresh{entityName}List');
                clearSelection();
                
                // Show success toast
                showToast('success', `Successfully deleted ${{ids.length}} {entityNameLower}(s)`);
            }} else {{
                showToast('error', 'Failed to delete items');
            }}
        }})
        .catch(error => {{
            console.error('Error:', error);
            showToast('error', 'An error occurred while deleting items');
        }});
    }}
    
    function showToast(type, message) {{
        // Simple toast notification
        const toast = document.createElement('div');
        toast.className = `alert alert-${{type}} fixed top-4 right-4 w-auto z-50 shadow-lg`;
        toast.innerHTML = `<span>${{message}}</span>`;
        document.body.appendChild(toast);
        setTimeout(() => toast.remove(), 3000);
    }}
</script>";
    }
}
