using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Swap.CLI.Commands.Relationships.Models;

namespace Swap.CLI.Commands.Relationships;

/// <summary>
/// Generates UI components for relationships (dropdowns, collections)
/// </summary>
public class RelationshipUIGenerator
{
    /// <summary>
    /// Detect relationships in an entity by analyzing properties
    /// Returns list of FK properties and their target entities
    /// </summary>
    public static async Task<List<DetectedRelationship>> DetectRelationshipsAsync(string entityPath)
    {
        var relationships = new List<DetectedRelationship>();
        
        if (!File.Exists(entityPath))
        {
            return relationships;
        }

        var code = await File.ReadAllTextAsync(entityPath);
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetCompilationUnitRoot();

        var classDeclaration = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault();

        if (classDeclaration == null)
        {
            return relationships;
        }

        var properties = classDeclaration.Members.OfType<PropertyDeclarationSyntax>();

        foreach (var prop in properties)
        {
            var propName = prop.Identifier.Text;
            var propType = prop.Type.ToString();

            // Detect FK properties (e.g., CustomerId, ProductId)
            if (propName.EndsWith("Id", StringComparison.Ordinal) && propName != "Id")
            {
                var targetEntity = propName.Substring(0, propName.Length - 2);
                
                // Check if corresponding navigation property exists
                var navProp = properties.FirstOrDefault(p => 
                    p.Identifier.Text == targetEntity && 
                    p.Type.ToString().Contains(targetEntity));

                if (navProp != null)
                {
                    relationships.Add(new DetectedRelationship
                    {
                        ForeignKeyProperty = propName,
                        TargetEntity = targetEntity,
                        NavigationProperty = targetEntity,
                        IsRequired = !propType.Contains("?"),
                        RelationshipType = DetectedRelationshipType.ManyToOne
                    });
                }
            }

            // Detect collection navigation properties (e.g., ICollection<Order>)
            if (propType.Contains("ICollection<") || propType.Contains("List<"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(
                    propType, @"(?:ICollection|List)<(\w+)>");
                
                if (match.Success)
                {
                    var relatedEntity = match.Groups[1].Value;
                    
                    // Check if this might be many-to-many by looking for reciprocal collection
                    // We'll mark as OneToMany for now, and detect ManyToMany in a second pass
                    relationships.Add(new DetectedRelationship
                    {
                        NavigationProperty = propName,
                        TargetEntity = relatedEntity,
                        RelationshipType = DetectedRelationshipType.OneToMany
                    });
                }
            }
        }

        // Second pass: Detect many-to-many relationships
        // A many-to-many exists when:
        // 1. This entity has ICollection<TargetEntity>
        // 2. TargetEntity also has ICollection<ThisEntity> (need to check target file)
        // 3. No FK property exists (distinguishes from one-to-many)
        
        var entityName = classDeclaration.Identifier.Text;
        var oneToManyRels = relationships.Where(r => r.RelationshipType == DetectedRelationshipType.OneToMany).ToList();
        
        foreach (var rel in oneToManyRels)
        {
            if (rel.TargetEntity == null) continue;
            
            // Check if target entity has a collection pointing back
            var targetEntityPath = Path.Combine(Path.GetDirectoryName(entityPath) ?? "", $"{rel.TargetEntity}.cs");
            if (File.Exists(targetEntityPath))
            {
                var targetCode = await File.ReadAllTextAsync(targetEntityPath);
                var targetTree = CSharpSyntaxTree.ParseText(targetCode);
                var targetRoot = targetTree.GetCompilationUnitRoot();
                var targetClass = targetRoot.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                
                if (targetClass != null)
                {
                    var targetProperties = targetClass.Members.OfType<PropertyDeclarationSyntax>();
                    
                    // Look for ICollection<CurrentEntity> in target
                    var reciprocalCollection = targetProperties.FirstOrDefault(p =>
                    {
                        var type = p.Type.ToString();
                        return (type.Contains($"ICollection<{entityName}>") || type.Contains($"List<{entityName}>"));
                    });
                    
                    // Check if there's NO FK pointing to target (which would indicate one-to-many instead)
                    var hasFkToTarget = properties.Any(p => 
                        p.Identifier.Text == $"{rel.TargetEntity}Id" && 
                        p.Type.ToString().Contains("int"));
                    
                    if (reciprocalCollection != null && !hasFkToTarget)
                    {
                        // This is many-to-many!
                        rel.RelationshipType = DetectedRelationshipType.ManyToMany;
                    }
                }
            }
        }
        
        return relationships;
    }

    /// <summary>
    /// Generate a dropdown select for a foreign key field
    /// </summary>
    public static string GenerateDropdownFormField(DetectedRelationship relationship, string displayField = "Name")
    {
        var fkName = relationship.ForeignKeyProperty ?? $"{relationship.TargetEntity}Id";
        // For self-reference, use navigation property name for better UX (e.g., "Parent" not "Category")
        var labelText = relationship.IsSelfReferencing 
            ? relationship.NavigationProperty 
            : relationship.TargetEntity;
        var label = FormatLabel(labelText ?? "Related");
        var required = relationship.IsRequired ? "required" : "";

        return $@"<div class=""form-control"">
    <label class=""label"">
        <span class=""label-text"">{label}</span>
    </label>
    <select name=""{fkName}"" 
            class=""select select-bordered w-full"" 
            {required}>
        <option value="""">-- Select {label} --</option>
        @foreach (var item in ViewBag.{relationship.TargetEntity}List as IEnumerable<dynamic>)
        {{
            <option value=""@item.Id"" selected=""@(Model.{fkName} == item.Id)"">
                @item.{displayField}
            </option>
        }}
    </select>
    <span asp-validation-for=""{fkName}"" class=""text-error text-sm""></span>
</div>";
    }

    /// <summary>
    /// Generate checkbox list for many-to-many relationship selection
    /// </summary>
    public static string GenerateCheckboxListFormField(DetectedRelationship relationship, string displayField = "Name")
    {
        var navProp = relationship.NavigationProperty ?? EntityModifier.Pluralize(relationship.TargetEntity ?? "Items");
        var label = FormatLabel(navProp);
        var targetEntity = relationship.TargetEntity;
        var selectedIdsVar = $"selected{targetEntity}Ids";

        return $@"<div class=""form-control mb-4"">
    <label class=""label"">
        <span class=""label-text font-semibold"">{label}</span>
    </label>
    <div class=""border border-base-300 rounded-lg p-4 max-h-64 overflow-y-auto"">
        @{{
            var {selectedIdsVar} = Model.{navProp}?.Select(x => x.Id).ToList() ?? new List<int>();
        }}
        @if ((ViewBag.{targetEntity}List as IEnumerable<dynamic>)?.Any() == true)
        {{
            @foreach (var item in ViewBag.{targetEntity}List as IEnumerable<dynamic>)
            {{
                var isChecked = {selectedIdsVar}.Contains((int)item.Id);
                <label class=""label cursor-pointer justify-start gap-2 py-2"">
                    <input type=""checkbox"" 
                           name=""Selected{targetEntity}Ids"" 
                           value=""@item.Id"" 
                           class=""checkbox checkbox-primary checkbox-sm""
                           checked=""@isChecked"" />
                    <span class=""label-text"">@item.{displayField}</span>
                </label>
            }}
        }}
        else
        {{
            <p class=""text-sm text-gray-500 italic"">No {targetEntity?.ToLower() ?? "items"}s available</p>
        }}
    </div>
</div>";
    }

    /// <summary>
    /// Generate controller code to populate ViewBag with dropdown data
    /// </summary>
    public static string GenerateViewBagPopulation(List<DetectedRelationship> relationships)
    {
        var code = new System.Text.StringBuilder();
        var addedEntities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // ManyToOne dropdowns
        foreach (var rel in relationships.Where(r => r.RelationshipType == DetectedRelationshipType.ManyToOne))
        {
            if (rel.TargetEntity != null && addedEntities.Add(rel.TargetEntity))
            {
                code.AppendLine($"        ViewBag.{rel.TargetEntity}List = await _context.{EntityModifier.Pluralize(rel.TargetEntity)}.ToListAsync();");
            }
        }

        // ManyToMany checkboxes
        foreach (var rel in relationships.Where(r => r.RelationshipType == DetectedRelationshipType.ManyToMany))
        {
            if (rel.TargetEntity != null && addedEntities.Add(rel.TargetEntity))
            {
                code.AppendLine($"        ViewBag.{rel.TargetEntity}List = await _context.{EntityModifier.Pluralize(rel.TargetEntity)}.ToListAsync();");
            }
        }

        return code.ToString();
    }

    /// <summary>
    /// Generate controller code to populate ViewBag for Edit action (excludes current entity for self-references)
    /// </summary>
    public static string GenerateViewBagPopulationForEdit(List<DetectedRelationship> relationships, string entityName)
    {
        var code = new System.Text.StringBuilder();
        var addedEntities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // ManyToOne dropdowns
        foreach (var rel in relationships.Where(r => r.RelationshipType == DetectedRelationshipType.ManyToOne))
        {
            if (rel.TargetEntity != null && addedEntities.Add(rel.TargetEntity))
            {
                if (rel.IsSelfReferencing)
                {
                    // For self-referencing, exclude the current entity to prevent circular reference
                    code.AppendLine($"        ViewBag.{rel.TargetEntity}List = await _context.{EntityModifier.Pluralize(rel.TargetEntity)}.Where(e => e.Id != id).ToListAsync();");
                }
                else
                {
                    // For normal relationships, include all entities
                    code.AppendLine($"        ViewBag.{rel.TargetEntity}List = await _context.{EntityModifier.Pluralize(rel.TargetEntity)}.ToListAsync();");
                }
            }
        }

        // ManyToMany checkboxes
        foreach (var rel in relationships.Where(r => r.RelationshipType == DetectedRelationshipType.ManyToMany))
        {
            if (rel.TargetEntity != null && addedEntities.Add(rel.TargetEntity))
            {
                code.AppendLine($"        ViewBag.{rel.TargetEntity}List = await _context.{EntityModifier.Pluralize(rel.TargetEntity)}.ToListAsync();");
            }
        }

        return code.ToString();
    }

    /// <summary>
    /// Generate code for Create action to handle many-to-many Selected{Entity}Ids
    /// </summary>
    public static string GenerateManyToManyCreateCode(List<DetectedRelationship> relationships)
    {
        var code = new System.Text.StringBuilder();
        var manyToManyRels = relationships.Where(r => r.RelationshipType == DetectedRelationshipType.ManyToMany).ToList();
        
        if (!manyToManyRels.Any())
        {
            return string.Empty;
        }

        code.AppendLine();
        code.AppendLine("        // Handle many-to-many relationships");
        
        foreach (var rel in manyToManyRels)
        {
            var navProp = rel.NavigationProperty ?? EntityModifier.Pluralize(rel.TargetEntity ?? "Items");
            var targetEntity = rel.TargetEntity;
            var targetPlural = EntityModifier.Pluralize(targetEntity ?? "Items");
            
            code.AppendLine($@"        if (selected{targetEntity}Ids != null && selected{targetEntity}Ids.Any())
        {{
            var selected{targetEntity} = await _context.{targetPlural}
                .Where(e => selected{targetEntity}Ids.Contains(e.Id))
                .ToListAsync();
            model.{navProp} = selected{targetEntity};
        }}");
        }
        
        return code.ToString();
    }

    /// <summary>
    /// Generate method parameters for Create/Edit actions to accept Selected{Entity}Ids
    /// </summary>
    public static string GenerateManyToManyParameters(List<DetectedRelationship> relationships)
    {
        var manyToManyRels = relationships.Where(r => r.RelationshipType == DetectedRelationshipType.ManyToMany).ToList();
        
        if (!manyToManyRels.Any())
        {
            return string.Empty;
        }

        var parameters = manyToManyRels
            .Select(r => $"int[]? selected{r.TargetEntity}Ids = null")
            .ToList();
        
        return ", " + string.Join(", ", parameters);
    }

    /// <summary>
    /// Generate code for Edit action to handle many-to-many Selected{Entity}Ids
    /// Includes loading existing relationships and updating the collection
    /// </summary>
    public static string GenerateManyToManyEditCode(List<DetectedRelationship> relationships, string entityName)
    {
        var code = new System.Text.StringBuilder();
        var manyToManyRels = relationships.Where(r => r.RelationshipType == DetectedRelationshipType.ManyToMany).ToList();
        
        if (!manyToManyRels.Any())
        {
            return string.Empty;
        }

        code.AppendLine();
        code.AppendLine("        // Handle many-to-many relationships");
        
        // Generate all Include statements for a single query
        var includes = string.Join("", manyToManyRels.Select(r => 
            $"\n            .Include(e => e.{r.NavigationProperty ?? EntityModifier.Pluralize(r.TargetEntity ?? "Items")})"));
        
        code.AppendLine($@"        // Load existing relationships for the entity being edited
        var existing{entityName} = await _context.{EntityModifier.Pluralize(entityName)}{includes}
            .FirstOrDefaultAsync(e => e.Id == id);
            
        if (existing{entityName} != null)
        {{");
        
        // Generate clear/add logic for each relationship
        foreach (var rel in manyToManyRels)
        {
            var navProp = rel.NavigationProperty ?? EntityModifier.Pluralize(rel.TargetEntity ?? "Items");
            var targetEntity = rel.TargetEntity;
            var targetPlural = EntityModifier.Pluralize(targetEntity ?? "Items");
            
            code.AppendLine($@"            // Clear existing {navProp}
            existing{entityName}.{navProp}.Clear();
            
            // Add new {navProp}
            if (selected{targetEntity}Ids != null && selected{targetEntity}Ids.Any())
            {{
                var selected{targetEntity} = await _context.{targetPlural}
                    .Where(e => selected{targetEntity}Ids.Contains(e.Id))
                    .ToListAsync();
                    
                foreach (var item in selected{targetEntity})
                {{
                    existing{entityName}.{navProp}.Add(item);
                }}
            }}
");
        }
        
        code.AppendLine("        }");
        
        return code.ToString();
    }

    /// <summary>
    /// Generate Include statements for eager loading many-to-many relationships in Edit action
    /// </summary>
    public static string GenerateManyToManyIncludes(List<DetectedRelationship> relationships)
    {
        var manyToManyRels = relationships.Where(r => r.RelationshipType == DetectedRelationshipType.ManyToMany).ToList();
        
        if (!manyToManyRels.Any())
        {
            return string.Empty;
        }

        var includes = manyToManyRels
            .Select(r => $".Include(e => e.{r.NavigationProperty ?? EntityModifier.Pluralize(r.TargetEntity ?? "Items")})")
            .ToList();
        
        return string.Join("", includes);
    }

    /// <summary>
    /// Generate display for related entity in detail/list views
    /// </summary>
    public static string GenerateRelationshipDisplay(DetectedRelationship relationship, string displayField = "Name")
    {
        if (relationship.RelationshipType == DetectedRelationshipType.ManyToOne)
        {
            var navProp = relationship.NavigationProperty ?? relationship.TargetEntity;
            var label = FormatLabel(relationship.TargetEntity ?? "Related");

            return $@"<div class=""mb-2"">
    <span class=""font-semibold"">{label}:</span>
    <span>@(Model.{navProp}?.{displayField} ?? ""None"")</span>
</div>";
        }
        else if (relationship.RelationshipType == DetectedRelationshipType.OneToMany)
        {
            var label = FormatLabel(relationship.NavigationProperty ?? "Items");

            return $@"<div class=""mb-4"">
    <span class=""font-semibold"">{label}:</span>
    <span class=""badge badge-primary"">@Model.{relationship.NavigationProperty}?.Count ?? 0</span>
</div>";
        }

        return string.Empty;
    }

    /// <summary>
    /// Auto-detect best display field for an entity
    /// Looks for Name, Title, Description, Email in that order
    /// </summary>
    public static async Task<string> DetectDisplayFieldAsync(string entityPath, string entityName)
    {
        if (!File.Exists(entityPath))
        {
            return "Id";
        }

        var code = await File.ReadAllTextAsync(entityPath);
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetCompilationUnitRoot();

        var classDeclaration = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(c => c.Identifier.Text == entityName);

        if (classDeclaration == null)
        {
            return "Id";
        }

        var properties = classDeclaration.Members
            .OfType<PropertyDeclarationSyntax>()
            .Select(p => p.Identifier.Text)
            .ToList();

        // Priority order for display fields
        var candidates = new[] { "Name", "Title", "Description", "Email", "Code", "Label" };
        
        foreach (var candidate in candidates)
        {
            if (properties.Contains(candidate, StringComparer.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        // Fallback to first string property
        var tree2 = CSharpSyntaxTree.ParseText(code);
        var root2 = tree2.GetCompilationUnitRoot();
        var class2 = root2.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        
        if (class2 != null)
        {
            var firstString = class2.Members
                .OfType<PropertyDeclarationSyntax>()
                .FirstOrDefault(p => p.Type.ToString() == "string");

            if (firstString != null)
            {
                return firstString.Identifier.Text;
            }
        }

        return "Id";
    }

    public static string FormatLabel(string name)
    {
        // Add spaces before capital letters: "CustomerId" -> "Customer Id"
        var result = System.Text.RegularExpressions.Regex.Replace(name, "([A-Z])", " $1").Trim();
        return result;
    }
}

/// <summary>
/// Represents a detected relationship in an entity
/// </summary>
public class DetectedRelationship
{
    public string? ForeignKeyProperty { get; set; }
    public string? TargetEntity { get; set; }
    public string? NavigationProperty { get; set; }
    public bool IsRequired { get; set; }
    public DetectedRelationshipType RelationshipType { get; set; }
    public bool IsSelfReferencing { get; set; }
}

/// <summary>
/// Type of detected relationship
/// </summary>
public enum DetectedRelationshipType
{
    ManyToOne,   // This entity has FK to another (needs dropdown)
    OneToMany,   // This entity has collection of others (display count/badges)
    ManyToMany   // This entity has collection that relates via junction table (checkboxes)
}
