namespace Swap.CLI.Infrastructure;

public class FieldDefinition
{
    public required string Name { get; set; }
    public required string Type { get; set; }
    public bool IsNullable { get; set; }
    public bool IsRequired { get; set; }
}

public static class FieldHelper
{
    /// <summary>
    /// Parse field specifications like "Name:string Title:string? Age:int" or "Name:string,Title:string?,Age:int"
    /// </summary>
    public static List<FieldDefinition> ParseFields(string? fieldsSpec)
    {
        var fields = new List<FieldDefinition>();
        
        if (string.IsNullOrWhiteSpace(fieldsSpec))
        {
            return fields;
        }
        
        // Split by both comma and space to handle both formats
        var separators = new[] { ',', ' ' };
        var fieldSpecs = fieldsSpec.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var spec in fieldSpecs)
        {
            var parts = spec.Split(':', 2);
            if (parts.Length != 2)
            {
                throw new ArgumentException($"Invalid field specification: {spec}. Expected format: 'FieldName:type'");
            }
            
            var fieldName = parts[0].Trim();
            var typeSpec = parts[1].Trim();
            
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
            
            fields.Add(new FieldDefinition
            {
                Name = fieldName,
                Type = mappedType,
                IsNullable = isNullable,
                IsRequired = mappedType == "string" ? !isNullable : !isNullable
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
    /// </summary>
    public static string GenerateTableHeader(FieldDefinition field)
    {
        return $"<th>{field.Name}</th>";
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
}
