using System.CommandLine;
using System.CommandLine.Invocation;
using Spectre.Console;
using Swap.CLI.Infrastructure;
using System.Text.RegularExpressions;

namespace Swap.CLI.Commands;

public static class GenerateSeedCommand
{
    public static Command Create()
    {
        var command = new Command("seed", "Generate database seeders using Bogus");
        command.AddAlias("s");

        var nameArg = new Argument<string>(
            name: "name",
            description: "Entity name (e.g., Post) or 'all' to generate for all DbSets");

        var countOption = new Option<int>("--count", () => 50, "Default number of records to seed");
        var localeOption = new Option<string>("--locale", () => "en", "Bogus locale (e.g., en, en_GB, de, fr)");
        var ifEmptyOption = new Option<bool>("--if-empty", description: "Only seed when table is empty");
        var appendOption = new Option<bool>("--append", description: "Append without clearing existing records (default; not yet implemented)");
        appendOption.IsHidden = true; // Hide until implemented
        
        var forceOption = new Option<bool>(
            aliases: new[] { "--force" },
            description: "Overwrite existing seeder files without prompting");
        
        var projectOption = new Option<string?>(
            aliases: new[] { "--project", "-p" },
            description: "Path to project directory (default: current directory)");

        command.AddArgument(nameArg);
        command.AddOption(countOption);
        command.AddOption(localeOption);
        command.AddOption(ifEmptyOption);
        command.AddOption(appendOption);
        command.AddOption(forceOption);
        command.AddOption(projectOption);

        command.SetHandler(async (InvocationContext ctx) =>
        {
            var name = ctx.ParseResult.GetValueForArgument(nameArg);
            var count = ctx.ParseResult.GetValueForOption(countOption);
            var locale = ctx.ParseResult.GetValueForOption(localeOption)!;
            var ifEmpty = ctx.ParseResult.GetValueForOption(ifEmptyOption);
            var append = ctx.ParseResult.GetValueForOption(appendOption);
            var force = ctx.ParseResult.GetValueForOption(forceOption);
            var projectPath = ctx.ParseResult.GetValueForOption(projectOption);

            ctx.ExitCode = await ExecuteAsync(name, count, locale, ifEmpty, append, force, projectPath);
        });

        return command;
    }

    private static async Task<int> ExecuteAsync(string name, int count, string locale, bool ifEmpty, bool append, bool force, string? projectPath)
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
            // Ensure Bogus package reference exists
            await EnsurePackageReferenceAsync(projectFile, "Bogus", "35.5.0");

            if (string.Equals(name, "all", StringComparison.OrdinalIgnoreCase))
            {
                var entities = await GetEntitiesFromDbContextAsync();
                if (!entities.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]No DbSet<T> entries found in Data/AppDbContext.cs[/]");
                    return 0;
                }

                AnsiConsole.MarkupLine($"[bold cyan]Generating seeders for:[/] {string.Join(", ", entities)}");
                foreach (var entity in entities)
                {
                    await GenerateSeederForEntityAsync(projectName, entity, count, locale, ifEmpty, append, force);
                }
            }
            else
            {
                // Single entity
                if (!char.IsUpper(name[0]))
                {
                    AnsiConsole.MarkupLine($"[yellow]Warning:[/] Entity name should start with uppercase. Using '{char.ToUpper(name[0]) + name.Substring(1)}'.");
                    name = char.ToUpper(name[0]) + name.Substring(1);
                }

                await GenerateSeederForEntityAsync(projectName, name, count, locale, ifEmpty, append, force);
            }

            AnsiConsole.MarkupLine("[green]✓[/] Seeder generation complete");
            AnsiConsole.MarkupLine("[dim]On development startup, SeedRunner will execute using env vars SEED_COUNT and SEED_LOCALE[/]");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    private static async Task GenerateSeederForEntityAsync(string projectName, string entityName, int count, string locale, bool ifEmpty, bool append, bool force)
    {
        // Read model file to infer fields
        var modelPath = Path.Combine("Models", $"{entityName}.cs");
        if (!File.Exists(modelPath))
            throw new FileNotFoundException($"Model not found: {modelPath}. Generate the model first (swap g m {entityName}) or provide it.");

        var modelContent = await File.ReadAllTextAsync(modelPath);
        var fields = SeedHelper.ParseModelProperties(modelContent);

        var (prelude, rules) = SeedHelper.GenerateFakerPreludeAndRules(projectName, entityName, fields);

        var templateDir = Path.Combine(AppContext.BaseDirectory, "templates", "generate", "seed");
        var templateFile = Path.Combine(templateDir, "EntitySeeder.cs.template");
        if (!File.Exists(templateFile))
            throw new FileNotFoundException($"Seeder template not found: {templateFile}");

        var templateContent = await File.ReadAllTextAsync(templateFile);

        var variables = new Dictionary<string, string>
        {
            { "ProjectName", projectName },
            { "EntityName", entityName },
            { "EntityNamePlural", entityName + "s" },
            { "SeedCount", count.ToString() },
            { "SeedLocale", locale },
            { "SeedIfEmpty", ifEmpty ? "true" : "false" },
            { "ForeignKeyPrelude", prelude },
            { "FakerRules", rules }
        };

        var processed = TemplateEngine.Process(templateContent, variables);

        var outPath = Path.Combine("Data", "Seeders");
        Directory.CreateDirectory(outPath);
        var seederFile = Path.Combine(outPath, $"{entityName}Seeder.cs");
        
        // Check for existing file if not forcing
        if (File.Exists(seederFile) && !force)
        {
            var overwrite = AnsiConsole.Confirm($"[yellow]File already exists:[/] {seederFile}\nOverwrite?", false);
            if (!overwrite)
            {
                AnsiConsole.MarkupLine($"[yellow]Skipped:[/] {entityName}Seeder");
                return;
            }
        }
        
        await File.WriteAllTextAsync(seederFile, processed);

        // Ensure SeedRunner exists and register
        await EnsureSeedRunnerAsync(projectName);
        await RegisterSeederCallAsync(entityName);
    }

    private static async Task EnsureSeedRunnerAsync(string projectName)
    {
        var runnerPath = Path.Combine("Data", "Seeders", "SeedRunner.cs");
        if (File.Exists(runnerPath)) return;

        var templateFile = Path.Combine(AppContext.BaseDirectory, "templates", "monolith", "Data", "Seeders", "SeedRunner.cs.template");
        if (!File.Exists(templateFile))
            throw new FileNotFoundException($"SeedRunner template not found: {templateFile}");

        var content = await File.ReadAllTextAsync(templateFile);
        var processed = TemplateEngine.Process(content, new Dictionary<string, string> { { "ProjectName", projectName } });
        Directory.CreateDirectory(Path.GetDirectoryName(runnerPath)!);
        await File.WriteAllTextAsync(runnerPath, processed);
    }

    private static async Task RegisterSeederCallAsync(string entityName)
    {
        var runnerPath = Path.Combine("Data", "Seeders", "SeedRunner.cs");
        var content = await File.ReadAllTextAsync(runnerPath);

        var sentinel = "// Add seeder calls here";
        if (!content.Contains(sentinel)) return; // unexpected, avoid corrupting file

        var call = $"        await {entityName}Seeder.SeedAsync(db, services, count, locale, ifEmpty);";

        if (content.Contains(call)) return; // already registered

        content = content.Replace(sentinel, call + "\n        " + sentinel);
        await File.WriteAllTextAsync(runnerPath, content);
    }

    private static async Task<List<string>> GetEntitiesFromDbContextAsync()
    {
        var path = Path.Combine("Data", "AppDbContext.cs");
        if (!File.Exists(path)) return new List<string>();
        var content = await File.ReadAllTextAsync(path);

        var list = new List<string>();
        var regex = new Regex(@"DbSet<(?<type>[A-Za-z0-9_\.]+)>\s+(?<prop>[A-Za-z0-9_]+)\s*{", RegexOptions.Compiled);
        foreach (Match m in regex.Matches(content))
        {
            var fullType = m.Groups["type"].Value;
            var typeName = fullType.Contains('.') ? fullType.Split('.').Last() : fullType;
            if (!string.Equals(typeName, "Migration", StringComparison.OrdinalIgnoreCase))
                list.Add(typeName);
        }
        return list.Distinct().ToList();
    }

    private static async Task EnsurePackageReferenceAsync(string csprojPath, string packageId, string version)
    {
        var xml = await File.ReadAllTextAsync(csprojPath);
        if (xml.Contains($"Include=\"{packageId}\"")) return;

        // Insert into the first ItemGroup with PackageReference or create new
        var itemGroupStart = xml.IndexOf("<ItemGroup>");
        var itemGroupEnd = itemGroupStart >= 0 ? xml.IndexOf("</ItemGroup>", itemGroupStart) : -1;
        var pkgLine = $"    <PackageReference Include=\"{packageId}\" Version=\"{version}\" />\n";

        if (itemGroupStart >= 0 && itemGroupEnd > itemGroupStart)
        {
            xml = xml.Insert(itemGroupEnd, pkgLine);
        }
        else
        {
            // Create new ItemGroup before end of Project
            var projectEnd = xml.IndexOf("</Project>");
            var block = $"  <ItemGroup>\n{pkgLine}  </ItemGroup>\n";
            if (projectEnd >= 0) xml = xml.Insert(projectEnd, block);
            else xml += "\n" + block;
        }

        await File.WriteAllTextAsync(csprojPath, xml);
    }
}
