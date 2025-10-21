using System.CommandLine;
using NetMX.CLI.Infrastructure;

namespace NetMX.CLI.Commands.Database;

/// <summary>
/// Command to rollback the last migration.
/// </summary>
public static class RollbackCommand
{
    public static Command Create()
    {
        var command = new Command("rollback", "Rollback the last applied migration");
        
        var stepsOption = new Option<int>("--steps", "-s")
        {
            Description = "Number of migrations to rollback (default: 1)"
        };
        
        var forceOption = new Option<bool>("--force", "-f")
        {
            Description = "Skip confirmation prompt"
        };

        command.Options.Add(stepsOption);
        command.Options.Add(forceOption);

        command.SetAction((parseResult) =>
        {
            var steps = parseResult.GetValue(stepsOption);
            var force = parseResult.GetValue(forceOption);
            return ExecuteAsync(steps, force).GetAwaiter().GetResult();
        });

        return command;
    }

    private static async Task<int> ExecuteAsync(int steps, bool force)
    {
        try
        {
            ConsoleHelper.WriteHeader("Rollback Migration");

            // Confirmation prompt (unless --force)
            if (!force)
            {
                ConsoleHelper.WriteWarning("⚠️  This will undo the last migration and update the database.");
                ConsoleHelper.WriteInfo($"   Migrations to rollback: {steps}");
                Console.Write("\nAre you sure? [y/N]: ");
                
                var response = Console.ReadLine()?.Trim().ToLowerInvariant();
                if (response != "y" && response != "yes")
                {
                    ConsoleHelper.WriteInfo("Rollback cancelled.");
                    return 0;
                }
            }

            var projectDir = Directory.GetCurrentDirectory();
            var orchestrator = new MigrationOrchestrator(projectDir, verbose: true);

            ConsoleHelper.WriteInfo("⏳ Rolling back migration...");
            
            var result = await orchestrator.RollbackMigrationOnlyAsync();

            if (result.IsSuccess)
            {
                ConsoleHelper.WriteSuccess("\n✅ Migration rolled back successfully!");
                
                foreach (var step in result.Steps)
                {
                    ConsoleHelper.WriteInfo($"   {step}");
                }
                
                return 0;
            }
            else
            {
                ConsoleHelper.WriteError($"Failed to rollback migration: {result.Message}");
                
                if (result.Steps.Any())
                {
                    ConsoleHelper.WriteInfo("\nSteps attempted:");
                    foreach (var step in result.Steps)
                    {
                        ConsoleHelper.WriteInfo($"   {step}");
                    }
                }
                
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
