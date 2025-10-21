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
                
                var command = new GenerateFeatureCommand(name!, module, search, export, migrate);
                return command.ExecuteAsync().GetAwaiter().GetResult();
            });
        }
        
        generateCommand.Subcommands.Add(generateFeatureCommand);
        generateCommand.Subcommands.Add(generateCrudCommand); // Keep as alias
        rootCommand.Subcommands.Add(generateCommand);

        // DB Command (Rails-inspired)
        var dbCommand = new Command("db", "Database management commands (migrate, update, rollback, seed, etc.)");
        
        // db migrate <name>
        var dbMigrateCommand = new Command("migrate", "Create a new database migration");
        var migrationNameArg = new Argument<string>("name") 
        { 
            Description = "Name of the migration (e.g., AddProduct, UpdateUsers)" 
        };
        dbMigrateCommand.Arguments.Add(migrationNameArg);
        dbMigrateCommand.SetAction((parseResult) =>
        {
            var name = parseResult.GetValue(migrationNameArg);
            var command = new DbCommand("migrate", name);
            return command.ExecuteAsync().GetAwaiter().GetResult();
        });
        
        // db update
        var dbUpdateCommand = new Command("update", "Apply pending migrations to the database");
        dbUpdateCommand.SetAction((parseResult) =>
        {
            var command = new DbCommand("update");
            return command.ExecuteAsync().GetAwaiter().GetResult();
        });
        
        // db rollback
        var dbRollbackCommand = new Command("rollback", "Undo the last migration");
        dbRollbackCommand.SetAction((parseResult) =>
        {
            var command = new DbCommand("rollback");
            return command.ExecuteAsync().GetAwaiter().GetResult();
        });
        
        // db reset
        var dbResetCommand = new Command("reset", "Drop and recreate the database");
        dbResetCommand.SetAction((parseResult) =>
        {
            var command = new DbCommand("reset");
            return command.ExecuteAsync().GetAwaiter().GetResult();
        });
        
        // db seed
        var dbSeedCommand = new Command("seed", "Run database seeders");
        dbSeedCommand.SetAction((parseResult) =>
        {
            var command = new DbCommand("seed");
            return command.ExecuteAsync().GetAwaiter().GetResult();
        });
        
        // db status
        var dbStatusCommand = new Command("status", "Show migration status");
        dbStatusCommand.SetAction((parseResult) =>
        {
            var command = new DbCommand("status");
            return command.ExecuteAsync().GetAwaiter().GetResult();
        });
        
        dbCommand.Subcommands.Add(dbMigrateCommand);
        dbCommand.Subcommands.Add(dbUpdateCommand);
        dbCommand.Subcommands.Add(dbRollbackCommand);
        dbCommand.Subcommands.Add(dbResetCommand);
        dbCommand.Subcommands.Add(dbSeedCommand);
        dbCommand.Subcommands.Add(dbStatusCommand);
        rootCommand.Subcommands.Add(dbCommand);

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