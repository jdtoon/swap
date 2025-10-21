using System.Text;
using NetMX.CLI.Models;

namespace NetMX.CLI.Infrastructure;

/// <summary>
/// Generates database seeder classes with ISeeder interface.
/// Produces seeder with repository injection, duplicate check, and sample data template.
/// </summary>
public static class SeederGenerator
{
    /// <summary>
    /// Generates complete seeder class code.
    /// </summary>
    /// <param name="options">Seeder generation options</param>
    /// <returns>Generated C# code as string</returns>
    public static string Generate(SeederGenerationOptions options)
    {
        var sb = new StringBuilder();

        // Using statements
        sb.AppendLine("using NetMX.Ddd.Domain.Repositories;");
        sb.AppendLine();

        // Namespace
        sb.AppendLine($"namespace {options.Namespace};");
        sb.AppendLine();

        // Class XML comment
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Seeder for {options.EntityName} entity");
        sb.AppendLine("/// </summary>");

        // Class declaration
        sb.AppendLine($"public class {options.SeederName} : ISeeder");
        sb.AppendLine("{");

        // Repository field
        sb.AppendLine($"    private readonly IQueryableRepository<{options.EntityName}, {options.KeyType}> _repository;");
        sb.AppendLine();

        // Constructor
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Constructor for {options.SeederName}");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine($"    public {options.SeederName}(IQueryableRepository<{options.EntityName}, {options.KeyType}> repository)");
        sb.AppendLine("    {");
        sb.AppendLine("        _repository = repository;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // SeedAsync method
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Seeds {options.EntityName} data if not already present");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public async Task SeedAsync()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Check if already seeded");
        sb.AppendLine("        if (await _repository.GetCountAsync() > 0)");
        sb.AppendLine("            return;");
        sb.AppendLine();
        sb.AppendLine("        // Add seed data here");
        sb.AppendLine("        var items = new[]");
        sb.AppendLine("        {");
        sb.AppendLine($"            new {options.EntityName}({options.KeyType}.NewGuid(), \"Sample {options.EntityName} 1\"),");
        sb.AppendLine($"            new {options.EntityName}({options.KeyType}.NewGuid(), \"Sample {options.EntityName} 2\"),");
        sb.AppendLine($"            new {options.EntityName}({options.KeyType}.NewGuid(), \"Sample {options.EntityName} 3\"),");
        sb.AppendLine("        };");
        sb.AppendLine();
        sb.AppendLine("        foreach (var item in items)");
        sb.AppendLine("        {");
        sb.AppendLine("            await _repository.InsertAsync(item);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");

        // Close class
        sb.AppendLine("}");

        return sb.ToString();
    }
}
