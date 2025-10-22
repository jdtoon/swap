using System.CommandLine;
using System.CommandLine.Parsing;
using System.Reflection;
using NetMX.CLI.Commands;
using NetMX.CLI.Infrastructure;

class Program
{
    static int Main(string[] args)
    {
        var rootCommand = new RootCommand("NetMX CLI - The best tooling for .NET + HTMX developers");

        // Display banner or version
        if (args.Contains("--version") || args.Contains("-v"))
        {
            DisplayVersion();
            return 0;
        }
        
        if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
        {
            DisplayBanner();
        }

        // New Command (Create new projects from templates)
        var newCommand = new Command("new", "Create a new NetMX project from a template");
        
        var newModularCommand = new Command("modular", "Create a new modular monolith project");
        var projectNameArg = new Argument<string>("name")
        {
            Description = "Name of the project (e.g., ECommerceApp)"
        };
        var outputOption = new Option<string?>("--output", "-o")
        {
            Description = "Output directory (default: ./{name})"
        };
        
        newModularCommand.Arguments.Add(projectNameArg);
        newModularCommand.Options.Add(outputOption);
        
        newModularCommand.SetAction((parseResult) =>
        {
            var name = parseResult.GetValue(projectNameArg);
            var output = parseResult.GetValue(outputOption);
            
            var command = new NewCommand("modular", name!, output);
            return command.ExecuteAsync().GetAwaiter().GetResult();
        });
        
        newCommand.Subcommands.Add(newModularCommand);
        rootCommand.Subcommands.Add(newCommand);

        // Add Module Command
        var addCommand = new Command("add", "Add items to your NetMX solution");
        
        var addModuleCommand = new Command("module", "Add a NetMX module to the current solution");
        var moduleNameArg = new Argument<string>("name") 
        { 
            Description = "Name of the module (e.g., Identity, Audit)" 
        };
        var sourceOption = new Option<string?>("--source") 
        { 
            Description = "Path to local module or 'nuget'" 
        };
        var skipMigrationOption = new Option<bool>("--skip-migration") 
        { 
            Description = "Skip running migrations" 
        };
        
        addModuleCommand.Arguments.Add(moduleNameArg);
        addModuleCommand.Options.Add(sourceOption);
        addModuleCommand.Options.Add(skipMigrationOption);
        
        addModuleCommand.SetAction((parseResult) =>
        {
            var name = parseResult.GetValue(moduleNameArg);
            var source = parseResult.GetValue(sourceOption);
            var skipMigration = parseResult.GetValue(skipMigrationOption);
            
            var command = new AddModuleCommand(name!, source, skipMigration);
            return command.ExecuteAsync().GetAwaiter().GetResult();
        });
        
        addCommand.Subcommands.Add(addModuleCommand);
        rootCommand.Subcommands.Add(addCommand);

        // Create Command
        var createCommand = new Command("create", "Create new items in your NetMX solution");
        
        var createModuleCommand = new Command("module", "Create a new NetMX module with 4-layer structure");
        var moduleNameArgCreate = new Argument<string>("name") 
        { 
            Description = "Name of the module (e.g., Audit, CMS)" 
        };
        
        createModuleCommand.Arguments.Add(moduleNameArgCreate);
        
        createModuleCommand.SetAction((parseResult) =>
        {
            var name = parseResult.GetValue(moduleNameArgCreate);
            var command = new CreateModuleCommand(name!);
            return command.ExecuteAsync().GetAwaiter().GetResult();
        });
        
        createCommand.Subcommands.Add(createModuleCommand);
        rootCommand.Subcommands.Add(createCommand);

        // Generate Command
        var generateCommand = new Command("generate", "Generate code for entities, controllers, views");
        
        // Keep 'crud' as alias for backward compatibility
        var generateFeatureCommand = new Command("feature", "Generate a complete feature (entity with CRUD operations)");
        var generateCrudCommand = new Command("crud", "Alias for 'feature' (deprecated, use 'feature' instead)");
        
        var entityNameArg = new Argument<string>("name") 
        { 
            Description = "Name of the entity (e.g., Product, Order)" 
        };
        var moduleOption = new Option<string?>("--module", "-m") 
        { 
            Description = "Target module name" 
        };
        var searchOption = new Option<bool>("--search") 
        { 
            Description = "Include search functionality" 
        };
        var exportOption = new Option<bool>("--export") 
        { 
            Description = "Include export to CSV" 
        };
        
        // Configure both commands with same arguments
        foreach (var cmd in new[] { generateFeatureCommand, generateCrudCommand })
        {
            cmd.Arguments.Add(new Argument<string>("name") 
            { 
                Description = "Name of the entity (e.g., Product, Order)" 
            });
            cmd.Options.Add(new Option<string?>("--module", "-m") 
            { 
                Description = "Target module name" 
            });
            cmd.Options.Add(new Option<bool>("--search") 
            { 
                Description = "Include search functionality" 
            });
            cmd.Options.Add(new Option<bool>("--export") 
            { 
                Description = "Include export to CSV" 
            });
            cmd.Options.Add(new Option<bool>("--migrate") 
            { 
                Description = "Automatically add DbSet, create migration, and update database (app context only)" 
            });
            
            cmd.SetAction((parseResult) =>
            {
                var name = parseResult.GetValue<string>("name");
                var module = parseResult.GetValue<string?>("--module");
                var search = parseResult.GetValue<bool>("--search");
                var export = parseResult.GetValue<bool>("--export");
                var migrate = parseResult.GetValue<bool>("--migrate");
                
                // For now, use default properties (name, description, isActive, createdAt, updatedAt)
                // Future: Add property parsing from CLI (--props flag)
                var command = new GenerateFeatureCommand(
                    entityName: name!,
                    module: module,
                    properties: null, // Use default properties
                    pageSize: search ? 20 : null, // Enable pagination if search is enabled
                    searchableProperties: search ? new List<string> { "Name", "Description" } : null,
                    filterableProperties: null,
                    sortableProperties: search ? new List<string> { "Name", "CreatedAt" } : null,
                    exportFormats: export ? new List<string> { "csv" } : null,
                    autoMigrate: migrate,
                    includeAuditFields: true,
                    includeSoftDelete: false
                );
                
                return command.ExecuteAsync().GetAwaiter().GetResult();
            });
        }
        
        generateCommand.Subcommands.Add(generateFeatureCommand);
        generateCommand.Subcommands.Add(generateCrudCommand); // Keep as alias
        
        // Generate Seeder Command
        var generateSeederCommand = new Command("seeder", "Generate a database seeder class");
        generateSeederCommand.Arguments.Add(new Argument<string>("name")
        {
            Description = "Name of the seeder (e.g., ProductSeeder)"
        });
        generateSeederCommand.Options.Add(new Option<string?>("--module", "-m")
        {
            Description = "Target module name (if generating in a module)"
        });
        generateSeederCommand.SetAction((parseResult) =>
        {
            var name = parseResult.GetValue<string>("name");
            var module = parseResult.GetValue<string?>("--module");
            
            var command = new GenerateSeederCommand(name!, module);
            return command.ExecuteAsync().GetAwaiter().GetResult();
        });
        
        generateCommand.Subcommands.Add(generateSeederCommand);
        rootCommand.Subcommands.Add(generateCommand);

        // DB Command - Use new structured commands from Database namespace
        var dbCommand = NetMX.CLI.Commands.Database.DatabaseCommand.Create();
        rootCommand.Subcommands.Add(dbCommand);

        // Test Command - Feature, module, and E2E testing
        var testCommand = NetMX.CLI.Commands.Test.TestCommand.Create();
        rootCommand.Subcommands.Add(testCommand);

        return rootCommand.Parse(args).Invoke();
    }

    private static void DisplayBanner()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(@"
 _   _      _   __  ____  __
| \ | | ___| |_|  \/  \ \/ /
|  \| |/ _ \ __| |\/| |\  / 
| |\  |  __/ |_| |  | |/  \ 
|_| \_|\___|\__|_|  |_/_/\_\
                            
");
        Console.ResetColor();
        ConsoleHelper.WriteInfo("The best CLI for .NET + HTMX developers");
        
        var version = GetVersion();
        ConsoleHelper.WriteInfo($"Version {version}");
        Console.WriteLine();
    }

    private static void DisplayVersion()
    {
        var version = GetVersion();
        var informationalVersion = GetInformationalVersion();
        
        Console.WriteLine($"NetMX CLI version {informationalVersion}");
        Console.WriteLine($"Assembly version: {version}");
        Console.WriteLine($".NET Runtime: {Environment.Version}");
    }

    private static string GetVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version?.ToString(3) ?? "0.1.0";
    }

    private static string GetInformationalVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        return attribute?.InformationalVersion ?? GetVersion();
    }
}