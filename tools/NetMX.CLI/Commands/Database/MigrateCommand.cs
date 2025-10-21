using System.CommandLine;
using NetMX.CLI.Infrastructure;

namespace NetMX.CLI.Commands.Database;

/// <summary>
/// Command to create a new EF Core migration.
/// </summary>
public static class MigrateCommand
{
    public static Command Create()
    {
        var command = new Command("migrate", "Create a new EF Core migration");
        
        var nameArg = new Argument<string>("name")
        {
            Description = "Name of the migration (e.g., AddProductDescription)"
        };
        
        var outputOption = new Option<string?>("--output-dir", "-o")
        {
            Description = "Output directory for migration files"
        };
        
        var contextOption = new Option<string?>("--context", "-c")
        {
            Description = "DbContext class name (auto-detected if not specified)"
        };

        command.Arguments.Add(nameArg);
        command.Options.Add(outputOption);
        command.Options.Add(contextOption);

        command.SetAction((parseResult) =>
        {
            var name = parseResult.GetValue(nameArg);
            var outputDir = parseResult.GetValue(outputOption);
            var context = parseResult.GetValue(contextOption);
            return ExecuteAsync(name!, outputDir, context).GetAwaiter().GetResult();
        });

        return command;
    }

    private static async Task<int> ExecuteAsync(string name, string? outputDir, string? context)
    {
        try
        {
            ConsoleHelper.WriteHeader($"Creating Migration: {name}");

            var projectDir = Directory.GetCurrentDirectory();
            var orchestrator = new MigrationOrchestrator(projectDir, verbose: true);

            // Create migration using orchestrator's public method
            var result = await orchestrator.CreateMigrationOnlyAsync(name);

            if (result.IsSuccess)
            {
                ConsoleHelper.WriteSuccess($"\n✅ {result.Message}");
                
                foreach (var step in result.Steps)
                {
                    ConsoleHelper.WriteInfo($"   {step}");
                }
                
                ConsoleHelper.WriteInfo("\n📋 Next steps:");
                ConsoleHelper.WriteInfo("   1. Review the generated migration file");
                ConsoleHelper.WriteInfo("   2. Run: netmx db update");
                ConsoleHelper.WriteInfo("   3. Or: dotnet ef database update");
                
                return 0;
            }
            else
            {
                ConsoleHelper.WriteError($"Failed to create migration: {result.Message}");
                ConsoleHelper.WriteInfo("\n💡 Troubleshooting:");
                ConsoleHelper.WriteInfo("   • Ensure EF Core tools are installed:");
                ConsoleHelper.WriteInfo("     dotnet tool install --global dotnet-ef");
                ConsoleHelper.WriteInfo("   • Ensure you're in the Web project directory");
                ConsoleHelper.WriteInfo("   • Check that DbContext exists and is configured");
                
                return 1;
            }
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Error: {ex.Message}");
            return 1;
        }
    }
}
