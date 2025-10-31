using System.Text.RegularExpressions;

namespace Swap.CLI.Infrastructure;

public static class SeedHelper
{
    private static readonly Regex PropertyRegex = new(
        pattern: @"public\s+(?:required\s+)?(?<type>[\w\.]+)(?<nullable>\?)?\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\{\s*get;\s*set;\s*\}",
        options: RegexOptions.Compiled);

    public static List<FieldDefinition> ParseModelProperties(string modelContent)
    {
        var fields = new List<FieldDefinition>();

        foreach (Match match in PropertyRegex.Matches(modelContent))
        {
            var name = match.Groups["name"].Value;
            if (name == "Id") continue; // Skip primary key

            var type = match.Groups["type"].Value;
            var isNullable = match.Groups["nullable"].Success;

            // Normalize simple type names (strip namespace if present)
            var simpleType = type.Contains('.') ? type.Split('.').Last() : type;

            // Map to FieldHelper types
            var mapped = simpleType switch
            {
                "String" or "string" => "string",
                "Int32" or "int" => "int",
                "Int64" or "long" => "long",
                "Int16" or "short" => "short",
                "Byte" or "byte" => "byte",
                "Boolean" or "bool" => "bool",
                "Single" or "float" => "float",
                "Double" or "double" => "double",
                "Decimal" or "decimal" => "decimal",
                "DateTime" => "DateTime",
                "Guid" => "Guid",
                _ => simpleType // treat as complex type; seeder will skip
            };

            fields.Add(new FieldDefinition
            {
                Name = name,
                Type = mapped,
                IsNullable = isNullable,
                IsRequired = !isNullable,
                IsSortable = true,
                IsFilterable = false
            });
        }

        return fields;
    }

    private static bool IsPatternProperty(string name)
    {
        // Skip common pattern properties - these are auto-managed by interceptors
        // Be specific to avoid false positives (e.g., PublishedAt should not match)
        return name is "CreatedAt" or "CreatedBy" or "UpdatedAt" or "UpdatedBy" 
            or "DeletedAt" or "DeletedBy" or "IsDeleted"
            or "Version" 
            or "IsVisible" or "VisibleFrom" or "VisibleUntil"
            or "Position";
    }

    public static (string Prelude, string Rules, string PostProcess) GenerateFakerPreludeAndRules(
        string projectName,
        string entityName,
        List<FieldDefinition> fields)
    {
        var prelude = new System.Text.StringBuilder();
        var rules = new System.Text.StringBuilder();
        var postProcess = new System.Text.StringBuilder();

        // Detect one-to-one relationships from AppDbContext.cs
        var oneToOneFields = DetectOneToOneForeignKeys(entityName);

        // Preload foreign key id lists
        foreach (var fk in DetectForeignKeys(fields))
        {
            var dbSet = fk.TargetEntity + "s"; // simple pluralization
            var varName = ToCamelCase(fk.TargetEntity) + "Ids";
            prelude.AppendLine($"        var {varName} = await db.{dbSet}.Select(x => x.Id).ToListAsync();");
            
            // For one-to-one relationships, shuffle the list for sequential assignment
            if (oneToOneFields.Contains(fk.FieldName))
            {
                prelude.AppendLine($"        {varName} = {varName}.OrderBy(x => Guid.NewGuid()).ToList(); // Shuffle for one-to-one");
            }
        }

        // For one-to-one relationships, limit count to available parent IDs
        if (oneToOneFields.Any())
        {
            var fksList = DetectForeignKeys(fields).Where(fk => oneToOneFields.Contains(fk.FieldName)).ToList();
            foreach (var fk in fksList)
            {
                var varName = ToCamelCase(fk.TargetEntity) + "Ids";
                prelude.AppendLine($"        count = Math.Min(count, {varName}.Count); // Limit to available {fk.TargetEntity} for one-to-one");
            }
        }

        // Advanced cross-field rule for UpdatedAt if CreatedAt exists
        var hasCreatedAt = fields.Any(f => f.Name == "CreatedAt" && f.Type == "DateTime");
        var hasUpdatedAt = fields.Any(f => f.Name == "UpdatedAt" && f.Type == "DateTime");

        foreach (var field in fields)
        {
            // Skip pattern properties - they're auto-managed by interceptors
            if (IsPatternProperty(field.Name))
                continue;

            // Skip complex navigation properties (non-primitive and not foreign key id)
            if (!IsSupportedPrimitive(field.Type) && !(IsForeignKey(field)))
                continue;

            var rule = GenerateRuleForField(projectName, field, fields, oneToOneFields);
            if (string.IsNullOrWhiteSpace(rule)) continue;

            rules.AppendLine($"            .RuleFor(x => x.{field.Name}, {rule})");
        }

        // If both CreatedAt and UpdatedAt exist, adjust UpdatedAt rule to depend on CreatedAt
        if (hasCreatedAt && hasUpdatedAt)
        {
            // Replace simple UpdatedAt rule with dependent rule
            rules = new System.Text.StringBuilder(
                rules.ToString().Replace(
                    ".RuleFor(x => x.UpdatedAt, f => f.Date.Between(DateTime.UtcNow.AddYears(-3), DateTime.UtcNow))",
                    ".RuleFor(x => x.UpdatedAt, (f,u) => f.Date.Between(u.CreatedAt, DateTime.UtcNow))"
                )
            );
        }

        // Generate post-processing code for one-to-one FK assignment
        if (oneToOneFields.Any())
        {
            postProcess.AppendLine();
            postProcess.AppendLine("        // Assign one-to-one foreign keys sequentially to ensure uniqueness");
            var fksList = DetectForeignKeys(fields).Where(fk => oneToOneFields.Contains(fk.FieldName)).ToList();
            foreach (var fk in fksList)
            {
                var varName = ToCamelCase(fk.TargetEntity) + "Ids";
                postProcess.AppendLine($"        for (int i = 0; i < items.Count; i++)");
                postProcess.AppendLine($"        {{");
                postProcess.AppendLine($"            items[i].{fk.FieldName} = {varName}[i];");
                postProcess.AppendLine($"        }}");
            }
        }

        return (prelude.ToString().TrimEnd(), rules.ToString().TrimEnd(), postProcess.ToString().TrimEnd());
    }

    private static bool IsSupportedPrimitive(string type) => type is "string" or "int" or "long" or "short" or "byte" or "bool" or "float" or "double" or "decimal" or "DateTime" or "Guid";

    private static bool IsForeignKey(FieldDefinition f) => (f.Name.EndsWith("Id") && f.Name != "Id") && (f.Type is "int" or "long" or "Guid");

    private static (string FieldName, string TargetEntity)[] DetectForeignKeys(List<FieldDefinition> fields)
    {
        return fields
            .Where(f => IsForeignKey(f) && !IsPatternProperty(f.Name)) // Exclude pattern properties
            .Select(f => (f.Name, TargetEntity: f.Name.Substring(0, f.Name.Length - 2)))
            .ToArray();
    }

    private static string GenerateRuleForField(string projectName, FieldDefinition field, List<FieldDefinition> all, HashSet<string> oneToOneFields)
    {
        // Foreign keys
        if (IsForeignKey(field))
        {
            // For one-to-one relationships, skip the RuleFor - we'll assign manually after generation
            if (oneToOneFields.Contains(field.Name))
            {
                return ""; // Skip rule for one-to-one FKs
            }
            
            // Normal one-to-many: use PickRandom
            var target = field.Name.Substring(0, field.Name.Length - 2);
            var varName = ToCamelCase(target) + "Ids";
            var pick = $"f => {varName}.Count > 0 ? f.PickRandom({varName}) : 0";
            if (field.Type is "long") pick = $"f => {varName}.Count > 0 ? (long)f.PickRandom({varName}) : 0L";
            if (field.Type is "Guid") pick = $"f => {varName}.Count > 0 ? f.PickRandom({varName}) : Guid.Empty";
            return WrapNullable(field, pick);
        }

        // Primitive types heuristics
        return field.Type switch
        {
            "string" => WrapNullable(field, StringRule(field.Name)),
            "int" => WrapNullable(field, IntRule(field.Name)),
            "long" => WrapNullable(field, LongRule(field.Name)),
            "short" => WrapNullable(field, "f => (short)f.Random.Int(0, 1000)"),
            "byte" => WrapNullable(field, "f => f.Random.Byte()"),
            "bool" => WrapNullable(field, BoolRule(field.Name)),
            "float" => WrapNullable(field, FloatRule(field.Name)),
            "double" => WrapNullable(field, DoubleRule(field.Name)),
            "decimal" => WrapNullable(field, DecimalRule(field.Name)),
            "DateTime" => WrapNullable(field, "f => f.Date.Between(DateTime.UtcNow.AddYears(-3), DateTime.UtcNow)"),
            "Guid" => WrapNullable(field, "f => Guid.NewGuid()"),
            _ => ""
        };
    }

    private static string WrapNullable(FieldDefinition field, string expr)
        => field.IsNullable ? $"f => f.Random.Bool(0.2f) ? null : ({expr.Replace("f => ", string.Empty)})" : expr;

    private static string ToCamelCase(string name) => char.ToLowerInvariant(name[0]) + name.Substring(1);

    private static string StringRule(string name)
    {
        var n = name.ToLowerInvariant();
        if (n.Contains("email")) return "f => f.Internet.Email()";
        if (n.Contains("url") || n.Contains("link") || n.Contains("website")) return "f => f.Internet.Url()";
        if (n.Contains("username") || n == "user" || n == "userName".ToLower()) return "f => f.Internet.UserName()";
        if (n == "firstname" || n == "first_name") return "f => f.Name.FirstName()";
        if (n == "lastname" || n == "last_name") return "f => f.Name.LastName()";
        if (n == "fullname" || n == "name") return "f => f.Name.FullName()";
        if (n.Contains("phone")) return "f => f.Phone.PhoneNumber()";
        if (n.Contains("address")) return "f => f.Address.FullAddress()";
        if (n.Contains("city")) return "f => f.Address.City()";
        if (n.Contains("state")) return "f => f.Address.State()";
        if (n.Contains("country")) return "f => f.Address.Country()";
        if (n.Contains("postal") || n.Contains("zip")) return "f => f.Address.ZipCode()";
        
        // Slug generation: use Bogus slug + unique suffix for uniqueness
        if (n.Contains("slug")) return "f => f.Lorem.Slug() + \"-\" + f.Random.AlphaNumeric(6)";
        
        if (n.Contains("image")) return "f => f.Image.PicsumUrl()";
        if (n.Contains("title")) return "f => f.Lorem.Sentence(4)";
        if (n.Contains("description") || n.Contains("summary") || n.Contains("body") || n.Contains("content")) return "f => f.Lorem.Paragraph()";
        return "f => f.Lorem.Word()";
    }

    private static string IntRule(string name)
    {
        var n = name.ToLowerInvariant();
        if (n.Contains("count") || n.Contains("quantity") || n.Contains("qty")) return "f => f.Random.Int(0, 500)";
        if (n.Contains("age")) return "f => f.Random.Int(18, 90)";
        return "f => f.Random.Int(0, 1000)";
    }

    private static string LongRule(string name) => "f => f.Random.Long(0, 1_000_000)";
    private static string FloatRule(string name) => "f => f.Random.Float(0, 1000)";
    private static string DoubleRule(string name) => "f => f.Random.Double(0, 1000)";
    private static string DecimalRule(string name)
    {
        var n = name.ToLowerInvariant();
        if (n.Contains("price") || n.Contains("amount") || n.Contains("total")) return "f => f.Finance.Amount(1, 1000)";
        return "f => Math.Round(f.Random.Decimal(0, 1000), 2)";
    }
    private static string BoolRule(string name)
    {
        var n = name.ToLowerInvariant();
        if (n.Contains("isactive") || n.Contains("active")) return "f => f.Random.Bool(0.7f)"; // 70% active
        if (n.Contains("isdeleted") || n.Contains("deleted")) return "f => f.Random.Bool(0.1f)"; // 10% deleted
        return "f => f.Random.Bool()";
    }

    /// <summary>
    /// Detects one-to-one foreign key fields for an entity by parsing AppDbContext.cs
    /// Returns a set of field names that are one-to-one FKs (e.g., "AuthorId")
    /// </summary>
    private static HashSet<string> DetectOneToOneForeignKeys(string entityName)
    {
        var oneToOneFields = new HashSet<string>();
        
        try
        {
            var dbContextPath = Path.Combine("Data", "AppDbContext.cs");
            if (!File.Exists(dbContextPath))
                return oneToOneFields;

            var content = File.ReadAllText(dbContextPath);
            
            // Look for patterns like:
            // modelBuilder.Entity<Author>().HasOne(e => e.AuthorProfile).WithOne(e => e.Author).HasForeignKey<AuthorProfile>(e => e.AuthorId)
            // Key indicator: WithOne() means one-to-one relationship
            
            var pattern = $@"\.HasOne\([^)]+\)\.WithOne\([^)]+\)\.HasForeignKey<{entityName}>\(e\s*=>\s*e\.(\w+)\)";
            var matches = Regex.Matches(content, pattern);
            
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    oneToOneFields.Add(match.Groups[1].Value);
                }
            }
        }
        catch
        {
            // If we can't detect, safely assume no one-to-one relationships
        }
        
        return oneToOneFields;
    }
}

