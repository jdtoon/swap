using Swap.CLI.Infrastructure;
using Swap.CLI.Models;

namespace Swap.CLI.Commands;

/// <summary>
/// Command to generate a database seeder class.
/// </summary>
public class GenerateSeederCommand
{
    private readonly string _name;
    private readonly string? _moduleName;

    public GenerateSeederCommand(string name, string? moduleName = null)
    {
        _name = name;
        _moduleName = moduleName;
    }

    public async Task<int> ExecuteAsync()
    {
        try
        {
            ConsoleHelper.WriteHeader($"Generating Seeder: {_name}");

            // Extract entity name (remove "Seeder" suffix if present)
            var entityName = _name.EndsWith("Seeder")
                ? _name.Substring(0, _name.Length - "Seeder".Length)
                : _name;
            var seederName = _name.EndsWith("Seeder") ? _name : $"{_name}Seeder";

            // Determine context (app vs module)
            string outputDirectory;
            string namespacePrefix;

            if (!string.IsNullOrEmpty(_moduleName))
            {
                // Module context - we're already in modules/{ModuleName}/{ModuleName}.Application
                // So we just need the Seeding folder here
                var currentDir = Directory.GetCurrentDirectory();
                
                // Check if we're already in the module's Application directory
                if (currentDir.EndsWith($"{_moduleName}.Application"))
                {
                    outputDirectory = Path.Combine(currentDir, "Seeding");
                    namespacePrefix = $"{_moduleName}.Application";
                }
                else
                {
                    // Try to find the module from repository root
                    var moduleBasePath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "..", "..", "modules", _moduleName, $"{_moduleName}.Application");

                    if (!Directory.Exists(moduleBasePath))
                    {
                        ConsoleHelper.WriteError($"Module not found: {_moduleName}");
                        ConsoleHelper.WriteInfo($"Expected path: {moduleBasePath}");
                        ConsoleHelper.WriteInfo("Make sure you're running from the module's Application directory or from the repository root");
                        return 1;
                    }

                    outputDirectory = Path.Combine(moduleBasePath, "Seeding");
                    namespacePrefix = $"{_moduleName}.Application";
                }
            }
            else
            {
                // App context
                var currentDir = Directory.GetCurrentDirectory();
                var projectName = Path.GetFileName(currentDir).Replace(".Web", "");
                
                outputDirectory = Path.Combine(currentDir, "Seeding");
                namespacePrefix = $"{projectName}.Web";
            }

            // Create options
            var options = new SeederGenerationOptions
            {
                SeederName = seederName,
                EntityName = entityName,
                Namespace = $"{namespacePrefix}.Seeding",
                ModuleName = _moduleName,
                KeyType = "Guid"
            };

            // Generate code
            ConsoleHelper.WriteStep(1, "Generating seeder class");
            var code = SeederGenerator.Generate(options);

            // Write file
            Directory.CreateDirectory(outputDirectory);
            var filePath = Path.Combine(outputDirectory, $"{seederName}.cs");
            await File.WriteAllTextAsync(filePath, code);

            ConsoleHelper.WriteSuccess($"Seeder '{seederName}' generated successfully!");
            ConsoleHelper.WriteInfo($"\nGenerated file:");
            ConsoleHelper.WriteInfo($"  {filePath}");

            // Show next steps
            Console.WriteLine();
            ConsoleHelper.WriteInfo("Next steps:");
            ConsoleHelper.WriteInfo("1. Customize the seed data in the SeedAsync method");
            ConsoleHelper.WriteInfo("2. Register the seeder in your startup code");
            ConsoleHelper.WriteInfo("3. Run: netmx db seed");

            return 0;
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Error: {ex.Message}");
            return 1;
        }
    }
}

