using System.Text.RegularExpressions;
using Swap.CLI.Models;

namespace Swap.CLI.Infrastructure;

/// <summary>
/// Parses property definitions from CLI input.
/// Examples:
///   - "name:string:256:required"
///   - "price:decimal:18:2:min:0:required"
///   - "status:enum:Draft,Published,Archived:default:Draft"
///   - "categoryId:guid:fk:Category:Name"
///   - "tagIds:guid[]:fk:Tag:Name"
/// </summary>
public class PropertyParser
{
    private static readonly Dictionary<string, string> TypeMappings = new()
    {
        { "string", "string" },
        { "text", "string" },
        { "int", "int" },
        { "long", "long" },
        { "decimal", "decimal" },
        { "double", "double" },
        { "float", "float" },
        { "bool", "bool" },
        { "boolean", "bool" },
        { "guid", "Guid" },
        { "datetime", "DateTime" },
        { "date", "DateTime" },
        { "time", "TimeSpan" }
    };

    /// <summary>
    /// Parses a single property definition string.
    /// Format: name:type[:length|precision:scale][:constraint1][:constraint2]...
    /// </summary>
    public static PropertyDefinition Parse(string input)
    {
        var property = new PropertyDefinition
        {
            RawInput = input
        };

        // Split by colon
        var parts = input.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length < 2)
        {
            throw new ArgumentException($"Invalid property definition: {input}. Expected format: name:type[:constraints]");
        }

        // 1. Property name
        property.Name = ToPascalCase(parts[0]);

        // 2. Type (with array support)
        var typePart = parts[1].ToLower();
        var isArray = typePart.EndsWith("[]");
        if (isArray)
        {
            property.IsCollection = true;
            typePart = typePart[..^2]; // Remove []
        }

        property.CliType = typePart;

        // Check if enum
        if (typePart == "enum")
        {
            property.IsEnum = true;
            property.Type = property.Name + "Enum"; // Will be generated
        }
        else if (TypeMappings.TryGetValue(typePart, out var csharpType))
        {
            property.Type = csharpType;
        }
        else
        {
            throw new ArgumentException($"Unknown type: {typePart}");
        }

        if (property.IsCollection)
        {
            property.Type = $"List<{property.Type}>";
        }

        // 3. Parse remaining parts (length, precision/scale, constraints)
        for (int i = 2; i < parts.Length; i++)
        {
            var part = parts[i].ToLower();

            // Check for keyword constraints
            switch (part)
            {
                case "required":
                    property.IsRequired = true;
                    break;
                
                case "min":
                    if (i + 1 < parts.Length)
                    {
                        property.MinValue = parts[++i];
                    }
                    break;
                
                case "max":
                    if (i + 1 < parts.Length)
                    {
                        property.MaxValue = parts[++i];
                    }
                    break;
                
                case "default":
                    if (i + 1 < parts.Length)
                    {
                        property.DefaultValue = parts[++i];
                    }
                    break;
                
                case "fk":
                    if (i + 1 < parts.Length)
                    {
                        property.ForeignKey = parts[++i];
                        property.IsNavigationProperty = true;
                        // Check if display property specified (next token is not a keyword)
                        if (i + 1 < parts.Length)
                        {
                            var nextPart = parts[i + 1].ToLower();
                            if (nextPart != "required" && nextPart != "min" && nextPart != "max" && nextPart != "default" && !int.TryParse(nextPart, out _))
                            {
                                property.ForeignKeyDisplay = parts[++i];
                            }
                        }
                    }
                    break;
                
                default:
                    // Enum values (comma-separated)
                    if (property.IsEnum && parts[i].Contains(','))
                    {
                        property.EnumValues = parts[i].Split(',', StringSplitOptions.TrimEntries).ToList();
                    }
                    // Length (for strings) or precision (for decimals)
                    else if (int.TryParse(parts[i], out var numValue))
                    {
                        if (property.Type == "string" && property.MaxLength == null)
                        {
                            property.MaxLength = numValue;
                        }
                        else if (property.Type == "decimal" && property.Precision == null)
                        {
                            property.Precision = numValue;
                        }
                        else if (property.Type == "decimal" && property.Scale == null)
                        {
                            property.Scale = numValue;
                        }
                    }
                    break;
            }
        }

        // Set nullable based on requirements
        if (!property.IsRequired && !property.IsCollection && property.Type != "string")
        {
            property.IsNullable = true;
        }

        return property;
    }

    /// <summary>
    /// Parses multiple property definitions.
    /// </summary>
    public static List<PropertyDefinition> ParseMultiple(IEnumerable<string> inputs)
    {
        return inputs.Select(Parse).ToList();
    }

    /// <summary>
    /// Converts a string to PascalCase (e.g., "categoryId" → "CategoryId")
    /// </summary>
    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Handle camelCase
        if (char.IsLower(input[0]))
        {
            return char.ToUpper(input[0]) + input[1..];
        }

        return input;
    }

    /// <summary>
    /// Generates C# property declaration with attributes.
    /// </summary>
    public static string GeneratePropertyDeclaration(PropertyDefinition prop)
    {
        var attributes = new List<string>();
        var lines = new List<string>();

        // Add attributes
        if (prop.IsRequired)
        {
            attributes.Add("[Required]");
        }

        if (prop.MaxLength.HasValue)
        {
            attributes.Add($"[MaxLength({prop.MaxLength.Value})]");
        }

        if (prop.MinValue != null || prop.MaxValue != null)
        {
            var min = prop.MinValue ?? (prop.Type == "decimal" ? "0" : "int.MinValue");
            var max = prop.MaxValue ?? (prop.Type == "decimal" ? "999999999" : "int.MaxValue");
            attributes.Add($"[Range({min}, {max})]");
        }

        if (prop.Type == "decimal" && prop.Precision.HasValue)
        {
            attributes.Add($"[Column(TypeName = \"decimal({prop.Precision},{prop.Scale ?? 2})\")]");
        }

        if (prop.ForeignKey != null)
        {
            attributes.Add($"[ForeignKey(nameof({prop.ForeignKey}))]");
        }

        // Add XML comment
        lines.Add($"    /// <summary>");
        lines.Add($"    /// {prop.Name}");
        lines.Add($"    /// </summary>");

        // Add attributes
        foreach (var attr in attributes)
        {
            lines.Add($"    {attr}");
        }

        // Property declaration
        var nullable = prop.IsNullable ? "?" : "";
        var defaultValue = prop.DefaultValue != null ? $" = {FormatDefaultValue(prop)};" : "";
        
        lines.Add($"    public {prop.Type}{nullable} {prop.Name} {{ get; private set; }}{defaultValue}");

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Formats default value for property initialization.
    /// </summary>
    private static string FormatDefaultValue(PropertyDefinition prop)
    {
        if (prop.DefaultValue == null)
            return "";

        return prop.Type switch
        {
            "string" => $"\"{prop.DefaultValue}\"",
            "bool" => prop.DefaultValue.ToLower(),
            "Guid" => "Guid.NewGuid()",
            "DateTime" => "DateTime.UtcNow",
            _ when prop.IsEnum => $"{prop.Type}.{prop.DefaultValue}",
            _ => prop.DefaultValue
        };
    }

    /// <summary>
    /// Generates constructor parameter.
    /// </summary>
    public static string GenerateConstructorParameter(PropertyDefinition prop)
    {
        var nullable = prop.IsNullable ? "?" : "";
        var camelCase = ToCamelCase(prop.Name);
        return $"{prop.Type}{nullable} {camelCase}";
    }

    /// <summary>
    /// Generates constructor assignment with validation.
    /// </summary>
    public static string GenerateConstructorAssignment(PropertyDefinition prop)
    {
        var camelCase = ToCamelCase(prop.Name);
        
        if (prop.IsRequired && prop.Type == "string")
        {
            return $"        {prop.Name} = Guard.NotNullOrEmpty({camelCase}, nameof({camelCase}));";
        }
        else if (prop.IsRequired)
        {
            return $"        {prop.Name} = Guard.NotNull({camelCase}, nameof({camelCase}));";
        }
        else
        {
            return $"        {prop.Name} = {camelCase};";
        }
    }

    /// <summary>
    /// Converts string to camelCase.
    /// </summary>
    private static string ToCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return char.ToLower(input[0]) + input[1..];
    }
}

