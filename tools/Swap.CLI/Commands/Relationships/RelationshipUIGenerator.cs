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
                    relationships.Add(new DetectedRelationship
                    {
                        NavigationProperty = propName,
                        TargetEntity = relatedEntity,
                        RelationshipType = DetectedRelationshipType.OneToMany
                    });
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
        var label = FormatLabel(relationship.TargetEntity ?? "Related");
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
    /// Generate controller code to populate ViewBag with dropdown data
    /// </summary>
    public static string GenerateViewBagPopulation(List<DetectedRelationship> relationships)
    {
        var code = new System.Text.StringBuilder();

        foreach (var rel in relationships.Where(r => r.RelationshipType == DetectedRelationshipType.ManyToOne))
        {
            code.AppendLine($"        ViewBag.{rel.TargetEntity}List = await _context.{EntityModifier.Pluralize(rel.TargetEntity!)}.ToListAsync();");
        }

        return code.ToString();
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

    private static string FormatLabel(string name)
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
}

/// <summary>
/// Type of detected relationship
/// </summary>
public enum DetectedRelationshipType
{
    ManyToOne,   // This entity has FK to another (needs dropdown)
    OneToMany    // This entity has collection of others (display count/badges)
}
