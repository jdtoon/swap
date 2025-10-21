using System.CommandLine;
using NetMX.CLI.Infrastructure;

namespace NetMX.CLI.Commands.Database;

/// <summary>
/// Command to apply pending migrations to the database.
/// </summary>
public static class UpdateCommand
{
    public static Command Create()
    {
        var command = new Command("update", "Apply pending migrations to the database");
        
        var targetOption = new Option<string?>("--target", "-t")
        {
            Description = "Target migration to apply (applies all if not specified)"
        };
        
        var contextOption = new Option<string?>("--context", "-c")
        {
            Description = "DbContext class name (auto-detected if not specified)"
        };

        command.Options.Add(targetOption);
        command.Options.Add(contextOption);

        command.SetAction((parseResult) =>
        {
            var target = parseResult.GetValue(targetOption);
            var context = parseResult.GetValue(contextOption);
            return ExecuteAsync(target, context).GetAwaiter().GetResult();
        });

        return command;
    }

    private static async Task<int> ExecuteAsync(string? target, string? context)
    {
        try
        {
            ConsoleHelper.WriteHeader("Applying Migrations");

            var projectDir = Directory.GetCurrentDirectory();
            var orchestrator = new MigrationOrchestrator(projectDir, verbose: true);

            ConsoleHelper.WriteInfo("⏳ Applying pending migrations...");
            
            var result = await orchestrator.UpdateDatabaseOnlyAsync();

            if (result.IsSuccess)
            {
                ConsoleHelper.WriteSuccess($"\n✅ {result.Message}");
                
                foreach (var step in result.Steps)
                {
                    ConsoleHelper.WriteInfo($"   {step}");
                }
                
                return 0;
            }
            else
            {
                ConsoleHelper.WriteError($"Failed to update database: {result.Message}");
                ConsoleHelper.WriteInfo("\n💡 Troubleshooting:");
                ConsoleHelper.WriteInfo("   • Check database connection string");
                ConsoleHelper.WriteInfo("   • Ensure database server is running");
                ConsoleHelper.WriteInfo("   • Review migration files for errors");
                ConsoleHelper.WriteInfo("   • Try: netmx db status (to see pending migrations)");
                
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
