using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Spectre.Console;
using Swap.CLI.Infrastructure;

namespace Swap.CLI.Commands;

public static class GenerateFactoryCommand
{
    public static Command Create()
    {
        var command = new Command("factory", "Generate Bogus-based test data factory for an entity");
        command.AddAlias("f");

        var nameArg = new Argument<string>(
            name: "entity",
            description: "Entity name (e.g., TodoItem)");

        var forceOption = new Option<bool>(
            aliases: new[] { "--force" },
            description: "Overwrite existing factory file without prompting");

        var projectOption = new Option<string?>(
            aliases: new[] { "--project", "-p" },
            description: "Path to project directory (default: current directory)");

        var outputOption = new Option<string?>(
            aliases: new[] { "--output", "-o" },
            description: "Output directory for factory file (default: Tests/Factories/)");

        command.AddArgument(nameArg);
        command.AddOption(forceOption);
        command.AddOption(projectOption);
        command.AddOption(outputOption);

        command.SetHandler(async (InvocationContext ctx) =>
        {
            var entity = ctx.ParseResult.GetValueForArgument(nameArg);
            var force = ctx.ParseResult.GetValueForOption(forceOption);
            var projectPath = ctx.ParseResult.GetValueForOption(projectOption);
            var outputPath = ctx.ParseResult.GetValueForOption(outputOption);

            ctx.ExitCode = await ExecuteAsync(entity, force, projectPath, outputPath);
        });

        return command;
    }

    public static async Task<int> ExecuteAsync(string entity, bool force, string? projectPath, string? outputPath)
    {
        // Resolve working directory
        var workingDir = !string.IsNullOrEmpty(projectPath)
            ? Path.GetFullPath(projectPath)
            : Directory.GetCurrentDirectory();

        // Validate cwd
        var projectFiles = Directory.GetFiles(workingDir, "*.csproj");
        if (projectFiles.Length == 0)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] No .csproj file found in {workingDir}. Run from your project root.");
            return 1;
        }

        var projectFile = projectFiles[0];
        var projectName = Path.GetFileNameWithoutExtension(projectFile);

        try
        {
            // Find entity model file
            var modelPath = Path.Combine(workingDir, "Models", $"{entity}.cs");
            if (!File.Exists(modelPath))
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Model file not found: {modelPath}");
                return 1;
            }

            // Parse entity properties
            var properties = await ParseEntityPropertiesAsync(modelPath);
            if (properties.Count == 0)
            {
                AnsiConsole.MarkupLine($"[yellow]Warning:[/] No properties found in {entity}");
            }

            // Ensure Bogus package reference
            await EnsureBogusPackageAsync(projectFile);

            // Determine output directory
            var factoryDir = !string.IsNullOrEmpty(outputPath)
                ? Path.Combine(workingDir, outputPath)
                : Path.Combine(workingDir, "Tests", "Factories");

            Directory.CreateDirectory(factoryDir);

            // Generate factory file
            var factoryFileName = $"{entity}Factory.cs";
            var factoryFilePath = Path.Combine(factoryDir, factoryFileName);

            // Check if file exists
            if (File.Exists(factoryFilePath) && !force)
            {
                var overwrite = AnsiConsole.Confirm($"[yellow]File {factoryFileName} already exists. Overwrite?[/]", false);
                if (!overwrite)
                {
                    AnsiConsole.MarkupLine("[yellow]Skipped.[/]");
                    return 0;
                }
            }

            // Load template
            var templatePath = Path.Combine(AppContext.BaseDirectory, "templates", "generate", "factory", "EntityFactory.cs.template");
            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Factory template not found: {templatePath}");

            var template = await File.ReadAllTextAsync(templatePath);

            // Generate faker rules
            var fakerRules = GenerateFakerRules(properties);

            // Replace placeholders
            var content = template
                .Replace("{{PROJECT_NAME}}", projectName)
                .Replace("{{ENTITY_NAME}}", entity)
                .Replace("{{FAKER_RULES}}", fakerRules);

            // Write file
            await File.WriteAllTextAsync(factoryFilePath, content);

            AnsiConsole.MarkupLine($"[green]✓[/] Generated [cyan]{factoryFileName}[/]");
            AnsiConsole.MarkupLine($"  [dim]→ {factoryFilePath}[/]");
            AnsiConsole.MarkupLine("");
            AnsiConsole.MarkupLine("[yellow]Usage example:[/]");
            AnsiConsole.MarkupLine($"  var factory = new {entity}Factory();");
            AnsiConsole.MarkupLine($"  var {entity.ToLower()} = factory.Generate();");
            AnsiConsole.MarkupLine($"  var {entity.ToLower()}s = factory.Generate(10);");

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    private static async Task EnsureBogusPackageAsync(string projectFile)
    {
        var content = await File.ReadAllTextAsync(projectFile);

        if (!content.Contains("Bogus"))
        {
            AnsiConsole.MarkupLine("[yellow]Note:[/] Bogus package not found. Add it with:");
            AnsiConsole.MarkupLine("  [cyan]dotnet add package Bogus[/]");
            AnsiConsole.MarkupLine("");
        }
    }

    private static async Task<List<PropertyInfo>> ParseEntityPropertiesAsync(string filePath)
    {
        var code = await File.ReadAllTextAsync(filePath);
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var properties = new List<PropertyInfo>();

        var classDeclaration = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault();

        if (classDeclaration == null)
            return properties;

        foreach (var property in classDeclaration.Members.OfType<PropertyDeclarationSyntax>())
        {
            var propertyName = property.Identifier.Text;
            var propertyType = property.Type.ToString();

            // Skip Id properties and navigation properties
            if (propertyName == "Id" || propertyType.Contains("List<") || propertyType.Contains("ICollection<"))
                continue;

            properties.Add(new PropertyInfo
            {
                Name = propertyName,
                Type = propertyType
            });
        }

        return properties;
    }

    private static string GenerateFakerRules(List<PropertyInfo> properties)
    {
        if (properties.Count == 0)
            return "";

        var rules = new List<string>();

        foreach (var prop in properties)
        {
            var rule = GenerateFakerRuleForProperty(prop);
            if (!string.IsNullOrEmpty(rule))
            {
                rules.Add($".RuleFor(x => x.{prop.Name}, {rule})");
            }
        }

        if (rules.Count == 0)
            return "";

        return "\n            " + string.Join("\n            ", rules);
    }

    private static string GenerateFakerRuleForProperty(PropertyInfo property)
    {
        var type = property.Type.TrimEnd('?'); // Remove nullable marker
        var name = property.Name.ToLower();

        // Common patterns based on property names
        if (name.Contains("email"))
            return "f => f.Internet.Email()";
        if (name.Contains("phone"))
            return "f => f.Phone.PhoneNumber()";
        if (name.Contains("firstname"))
            return "f => f.Name.FirstName()";
        if (name.Contains("lastname"))
            return "f => f.Name.LastName()";
        if (name.Contains("name"))
            return "f => f.Name.FullName()";
        if (name.Contains("address"))
            return "f => f.Address.FullAddress()";
        if (name.Contains("city"))
            return "f => f.Address.City()";
        if (name.Contains("country"))
            return "f => f.Address.Country()";
        if (name.Contains("zip") || name.Contains("postal"))
            return "f => f.Address.ZipCode()";
        if (name.Contains("url") || name.Contains("website"))
            return "f => f.Internet.Url()";
        if (name.Contains("username"))
            return "f => f.Internet.UserName()";
        if (name.Contains("company"))
            return "f => f.Company.CompanyName()";
        if (name.Contains("title") || name.Contains("heading"))
            return "f => f.Lorem.Sentence()";
        if (name.Contains("description") || name.Contains("content") || name.Contains("body"))
            return "f => f.Lorem.Paragraphs(2)";
        if (name.Contains("price") || name.Contains("amount") || name.Contains("cost"))
            return "f => f.Random.Decimal(1, 1000)";
        if (name.Contains("quantity") || name.Contains("count"))
            return "f => f.Random.Int(1, 100)";

        // Type-based defaults
        return type switch
        {
            "string" => "f => f.Lorem.Word()",
            "int" => "f => f.Random.Int(1, 1000)",
            "long" => "f => f.Random.Long(1, 10000)",
            "decimal" => "f => f.Random.Decimal(0, 1000)",
            "double" => "f => f.Random.Double(0, 1000)",
            "float" => "f => f.Random.Float(0, 1000)",
            "bool" => "f => f.Random.Bool()",
            "DateTime" => "f => f.Date.Past()",
            "DateTimeOffset" => "f => f.Date.PastOffset()",
            "Guid" => "f => Guid.NewGuid()",
            _ => $"f => default({type})  // TODO: Customize this rule"
        };
    }

    private class PropertyInfo
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
    }
}
