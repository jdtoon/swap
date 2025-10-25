using System.Diagnostics;
using System.Text;

namespace Swap.CLI.Infrastructure;

/// <summary>
/// Wraps dotnet ef commands with better UX and error handling.
/// </summary>
public class MigrationRunner
{
    /// <summary>
    /// Creates a new EF Core migration.
    /// </summary>
    public static async Task<bool> CreateMigrationAsync(string migrationName, string? projectPath = null)
    {
        projectPath ??= Directory.GetCurrentDirectory();

        Console.WriteLine($"🔄 Creating migration: {migrationName}");

        var args = $"ef migrations add {migrationName}";
        
        var (success, output) = await RunDotnetCommandAsync(args, projectPath);

        if (success)
        {
            Console.WriteLine($"✅ Migration '{migrationName}' created successfully");
            return true;
        }
        else
        {
            Console.WriteLine($"❌ Failed to create migration:");
            Console.WriteLine(output);
            return false;
        }
    }

    /// <summary>
    /// Applies all pending migrations to the database.
    /// </summary>
    public static async Task<bool> UpdateDatabaseAsync(string? projectPath = null)
    {
        projectPath ??= Directory.GetCurrentDirectory();

        Console.WriteLine($"🔄 Applying migrations to database...");

        var args = "ef database update";
        
        var (success, output) = await RunDotnetCommandAsync(args, projectPath);

        if (success)
        {
            Console.WriteLine($"✅ Database updated successfully");
            return true;
        }
        else
        {
            Console.WriteLine($"❌ Failed to update database:");
            Console.WriteLine(output);
            return false;
        }
    }

    /// <summary>
    /// Lists all migrations in the project.
    /// </summary>
    public static async Task<List<string>> ListMigrationsAsync(string? projectPath = null)
    {
        projectPath ??= Directory.GetCurrentDirectory();

        var args = "ef migrations list";
        var (success, output) = await RunDotnetCommandAsync(args, projectPath);

        if (success)
        {
            return output
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => !line.StartsWith("Build") && !string.IsNullOrWhiteSpace(line))
                .ToList();
        }

        return new List<string>();
    }

    /// <summary>
    /// Removes the last migration.
    /// </summary>
    public static async Task<bool> RemoveMigrationAsync(string? projectPath = null)
    {
        projectPath ??= Directory.GetCurrentDirectory();

        Console.WriteLine($"🔄 Removing last migration...");

        var args = "ef migrations remove";
        var (success, output) = await RunDotnetCommandAsync(args, projectPath);

        if (success)
        {
            Console.WriteLine($"✅ Last migration removed successfully");
            return true;
        }
        else
        {
            Console.WriteLine($"❌ Failed to remove migration:");
            Console.WriteLine(output);
            return false;
        }
    }

    /// <summary>
    /// Checks if EF Core tools are installed.
    /// </summary>
    public static async Task<bool> IsEfCoreInstalledAsync()
    {
        var (success, output) = await RunDotnetCommandAsync("ef --version", Directory.GetCurrentDirectory());
        return success && output.Contains("Entity Framework Core");
    }

    /// <summary>
    /// Runs a dotnet command and captures output.
    /// </summary>
    private static async Task<(bool success, string output)> RunDotnetCommandAsync(string arguments, string workingDirectory)
    {
        // Validate directory exists
        if (!Directory.Exists(workingDirectory))
        {
            return (false, $"Directory not found: {workingDirectory}");
        }

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            var output = new StringBuilder();
            var error = new StringBuilder();

            process.OutputDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                    output.AppendLine(args.Data);
            };

            process.ErrorDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                    error.AppendLine(args.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            var allOutput = output.ToString() + error.ToString();
            return (process.ExitCode == 0, allOutput);
        }
        catch (Exception ex)
        {
            return (false, $"Error running dotnet command: {ex.Message}");
        }
    }
}

