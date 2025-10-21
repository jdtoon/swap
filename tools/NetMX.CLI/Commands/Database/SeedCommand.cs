using System.CommandLine;
using NetMX.CLI.Infrastructure;

namespace NetMX.CLI.Commands.Database;

/// <summary>
/// Command to run database seeders.
/// Seeds initial data into the database.
/// </summary>
public static class SeedCommand
{
    public static Command Create()
    {
        var command = new Command("seed", "Run database seeders");
        
        var seederOption = new Option<string?>("--seeder", "-s")
        {
            Description = "Specific seeder to run (runs all if not specified)"
        };

        command.Options.Add(seederOption);

        command.SetAction((parseResult) =>
        {
            var seeder = parseResult.GetValue(seederOption);
            return ExecuteAsync(seeder).GetAwaiter().GetResult();
        });

        return command;
    }

    private static async Task<int> ExecuteAsync(string? seeder)
    {
        try
        {
            ConsoleHelper.WriteHeader("Running Seeders");

            // Find project directory
            var projectPath = Directory.GetCurrentDirectory();
            var projectFile = Directory.GetFiles(projectPath, "*.csproj").FirstOrDefault();
            
            if (projectFile == null)
            {
                ConsoleHelper.WriteError("❌ No .csproj file found in current directory");
                return 1;
            }

            ConsoleHelper.WriteInfo($"Project: {Path.GetFileNameWithoutExtension(projectFile)}");

            // Create seeder executor
            var executor = new SeederExecutor(projectPath);

            // Discover seeders
            ConsoleHelper.WriteInfo("\n🔍 Discovering seeders...");
            var seeders = await executor.DiscoverSeedersAsync();

            if (seeders.Count == 0)
            {
                ConsoleHelper.WriteWarning("⚠️  No seeders found");
                ConsoleHelper.WriteInfo("\nSearched in:");
                ConsoleHelper.WriteInfo("  • Data/Seeders/");
                ConsoleHelper.WriteInfo("  • Database/Seeders/");
                ConsoleHelper.WriteInfo("  • Seeders/");
                ConsoleHelper.WriteInfo("  • Data/");
                ConsoleHelper.WriteInfo("\n💡 Tip: Use 'netmx generate seeder' to create seeder classes");
                return 0;
            }

            ConsoleHelper.WriteInfo($"Found {seeders.Count} seeder(s):");
            foreach (var s in seeders)
            {
                ConsoleHelper.WriteInfo($"  • {s}");
            }

            // Run seeders
            ConsoleHelper.WriteInfo("\n▶ Running seeders...");
            Console.WriteLine(new string('─', 60));

            var result = await executor.RunSeedersAsync(seeder);

            Console.WriteLine(new string('─', 60));

            if (result.Success)
            {
                ConsoleHelper.WriteSuccess($"✅ Successfully ran {result.SeedersRun.Count} seeder(s)");
                return 0;
            }
            else
            {
                ConsoleHelper.WriteError($"❌ Seeding failed: {result.ErrorMessage}");
                return 1;
            }
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"❌ Error: {ex.Message}");
            if (ex.InnerException != null)
            {
                ConsoleHelper.WriteError($"   {ex.InnerException.Message}");
            }
            return 1;
        }
    }
}
