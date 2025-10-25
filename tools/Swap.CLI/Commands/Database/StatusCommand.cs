using System.CommandLine;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Swap.CLI.Infrastructure;

namespace Swap.CLI.Commands.Database;

/// <summary>
/// Command to show database migration status.
/// </summary>
public static class StatusCommand
{
    public static Command Create()
    {
        var command = new Command("status", "Show database migration status");
        
        var contextOption = new Option<string?>("--context", "-c")
        {
            Description = "DbContext class name (auto-detected if not specified)"
        };

        command.Options.Add(contextOption);

        command.SetAction((parseResult) =>
        {
            var context = parseResult.GetValue(contextOption);
            return ExecuteAsync(context).GetAwaiter().GetResult();
        });

        return command;
    }

    private static async Task<int> ExecuteAsync(string? context)
    {
        try
        {
            ConsoleHelper.WriteHeader("Migration Status");

            var projectDir = Directory.GetCurrentDirectory();

            // Get list of migrations
            var listResult = await RunEfCommandAsync("migrations list", projectDir);
            
            if (!listResult.success)
            {
                ConsoleHelper.WriteError("Failed to get migration list");
                ConsoleHelper.WriteInfo($"Error: {listResult.output}");
                return 1;
            }

            // Parse migrations (format: "migrationId (Applied)" or "migrationId (Pending)")
            var migrations = ParseMigrations(listResult.output);

            if (!migrations.Any())
            {
                ConsoleHelper.WriteWarning("⚠️  No migrations found");
                ConsoleHelper.WriteInfo("\n💡 Create your first migration:");
                ConsoleHelper.WriteInfo("   netmx db migrate InitialCreate");
                return 0;
            }

            // Display status
            ConsoleHelper.WriteInfo("\n📊 Migration Status");
            ConsoleHelper.WriteInfo("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

            var applied = migrations.Where(m => m.isApplied).ToList();
            var pending = migrations.Where(m => !m.isApplied).ToList();

            if (applied.Any())
            {
                ConsoleHelper.WriteSuccess("\nApplied:");
                foreach (var migration in applied)
                {
                    ConsoleHelper.WriteInfo($"  ✅ {migration.name}");
                }
            }

            if (pending.Any())
            {
                ConsoleHelper.WriteWarning("\nPending:");
                foreach (var migration in pending)
                {
                    ConsoleHelper.WriteInfo($"  ⏳ {migration.name}");
                }
                
                ConsoleHelper.WriteInfo("\n💡 Apply pending migrations:");
                ConsoleHelper.WriteInfo("   netmx db update");
            }

            // Summary
            ConsoleHelper.WriteInfo($"\nTotal: {applied.Count} applied, {pending.Count} pending");

            if (!pending.Any())
            {
                ConsoleHelper.WriteSuccess("\n✅ Database is up to date!");
            }

            return 0;
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"Error: {ex.Message}");
            return 1;
        }
    }

    private static List<(string name, bool isApplied)> ParseMigrations(string output)
    {
        var migrations = new List<(string, bool)>();
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            // Skip header lines
            if (trimmed.Contains("Build started") || 
                trimmed.Contains("Build succeeded") || 
                trimmed.Contains("migrations:") ||
                string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            // Parse migration line (format varies by EF Core version)
            // Example: "20241021120000_AddProduct (Applied)" or "20241021120000_AddProduct (Pending)"
            var isApplied = trimmed.Contains("(Applied)", StringComparison.OrdinalIgnoreCase);
            var name = trimmed
                .Replace("(Applied)", "")
                .Replace("(Pending)", "")
                .Trim();

            if (!string.IsNullOrWhiteSpace(name))
            {
                migrations.Add((name, isApplied));
            }
        }

        return migrations;
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

