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

    public static (string Prelude, string Rules) GenerateFakerPreludeAndRules(
        string projectName,
        string entityName,
        List<FieldDefinition> fields)
    {
        var prelude = new System.Text.StringBuilder();
        var rules = new System.Text.StringBuilder();

        // Preload foreign key id lists
        foreach (var fk in DetectForeignKeys(fields))
        {
            var dbSet = fk.TargetEntity + "s"; // simple pluralization
            var varName = ToCamelCase(fk.TargetEntity) + "Ids";
            prelude.AppendLine($"        var {varName} = await db.{dbSet}.Select(x => x.Id).ToListAsync();");
        }

        // Advanced cross-field rule for UpdatedAt if CreatedAt exists
        var hasCreatedAt = fields.Any(f => f.Name == "CreatedAt" && f.Type == "DateTime");
        var hasUpdatedAt = fields.Any(f => f.Name == "UpdatedAt" && f.Type == "DateTime");

        foreach (var field in fields)
        {
            // Skip complex navigation properties (non-primitive and not foreign key id)
            if (!IsSupportedPrimitive(field.Type) && !(IsForeignKey(field)))
                continue;

            var rule = GenerateRuleForField(projectName, field, fields);
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

        return (prelude.ToString().TrimEnd(), rules.ToString().TrimEnd());
    }

    private static bool IsSupportedPrimitive(string type) => type is "string" or "int" or "long" or "short" or "byte" or "bool" or "float" or "double" or "decimal" or "DateTime" or "Guid";

    private static bool IsForeignKey(FieldDefinition f) => (f.Name.EndsWith("Id") && f.Name != "Id") && (f.Type is "int" or "long" or "Guid");

    private static (string FieldName, string TargetEntity)[] DetectForeignKeys(List<FieldDefinition> fields)
    {
        return fields
            .Where(IsForeignKey)
            .Select(f => (f.Name, TargetEntity: f.Name.Substring(0, f.Name.Length - 2)))
            .ToArray();
    }

    private static string GenerateRuleForField(string projectName, FieldDefinition field, List<FieldDefinition> all)
    {
        // Foreign keys
        if (IsForeignKey(field))
        {
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
        => field.IsNullable ? $"f => f.Random.Bool(1,5) == 1 ? null : ({expr.Replace("f => ", string.Empty)})" : expr;

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
        if (n.Contains("slug")) return "f => f.Lorem.Slug()";
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
}
