using System.CommandLine;
using System.Diagnostics;
using NetMX.CLI.Infrastructure;

namespace NetMX.CLI.Commands.Database;

/// <summary>
/// Command to drop and recreate the database.
/// </summary>
public static class ResetCommand
{
    public static Command Create()
    {
        var command = new Command("reset", "Drop and recreate the database (⚠️ DESTRUCTIVE)");
        
        var forceOption = new Option<bool>("--force", "-f")
        {
            Description = "Skip confirmation prompt"
        };
        
        var seedOption = new Option<bool>("--seed")
        {
            Description = "Run seeders after reset"
        };

        command.Options.Add(forceOption);
        command.Options.Add(seedOption);

        command.SetAction((parseResult) =>
        {
            var force = parseResult.GetValue(forceOption);
            var seed = parseResult.GetValue(seedOption);
            return ExecuteAsync(force, seed).GetAwaiter().GetResult();
        });

        return command;
    }

    private static async Task<int> ExecuteAsync(bool force, bool seed)
    {
        try
        {
            ConsoleHelper.WriteHeader("Reset Database");

            // Strong warning
            if (!force)
            {
                ConsoleHelper.WriteError("⚠️  WARNING: This will DELETE ALL DATA in the database!");
                ConsoleHelper.WriteWarning("   This action cannot be undone.");
                Console.Write("\nType 'DELETE' to confirm: ");
                
                var response = Console.ReadLine()?.Trim();
                if (response != "DELETE")
                {
                    ConsoleHelper.WriteInfo("Reset cancelled.");
                    return 0;
                }
            }

            var projectDir = Directory.GetCurrentDirectory();

            // Step 1: Drop database
            ConsoleHelper.WriteInfo("\n[1/2] Dropping database...");
            var dropResult = await RunEfCommandAsync("database drop --force", projectDir);
            
            if (!dropResult.success)
            {
                ConsoleHelper.WriteError($"Failed to drop database: {dropResult.output}");
                return 1;
            }
            
            ConsoleHelper.WriteSuccess("   ✅ Database dropped");

            // Step 2: Apply all migrations
            ConsoleHelper.WriteInfo("[2/2] Applying all migrations...");
            var updateResult = await RunEfCommandAsync("database update", projectDir);
            
            if (!updateResult.success)
            {
                ConsoleHelper.WriteError($"Failed to update database: {updateResult.output}");
                return 1;
            }
            
            ConsoleHelper.WriteSuccess("   ✅ Migrations applied");

            // Step 3: Run seeders (if requested)
            if (seed)
            {
                ConsoleHelper.WriteInfo("\n[3/3] Running seeders...");
                ConsoleHelper.WriteWarning("   ⚠️  Seeder execution not implemented yet");
                ConsoleHelper.WriteInfo("   Seeders will be available in CLI Phase 2D");
            }

            ConsoleHelper.WriteSuccess("\n✅ Database reset complete!");
            ConsoleHelper.WriteInfo("   Database is now fresh with all migrations applied");
            
            return 0;
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Error: {ex.Message}");
            return 1;
        }
    }

    private static async Task<(bool success, string output)> RunEfCommandAsync(string arguments, string workingDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"ef {arguments}",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        
        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();

        process.OutputDataReceived += (sender, args) =>
        {
            if (args.Data != null) outputBuilder.AppendLine(args.Data);
        };

        process.ErrorDataReceived += (sender, args) =>
        {
            if (args.Data != null) errorBuilder.AppendLine(args.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        var output = outputBuilder.ToString();
        var error = errorBuilder.ToString();

        return (process.ExitCode == 0, string.IsNullOrEmpty(error) ? output : error);
    }
}
