using System.Diagnostics;
using System.Text;

namespace Swap.CLI.Infrastructure;

/// <summary>
/// Orchestrates the complete entity addition workflow:
/// 1. Add DbSet to DbContext
/// 2. Create EF Core migration
/// 3. Apply migration to database
/// </summary>
public class MigrationOrchestrator
{
    private readonly string _projectDirectory;
    private readonly bool _verbose;

    public MigrationOrchestrator(string projectDirectory, bool verbose = false)
    {
        _projectDirectory = projectDirectory ?? throw new ArgumentNullException(nameof(projectDirectory));
        _verbose = verbose;
    }

    /// <summary>
    /// Adds an entity to the DbContext and optionally creates/applies migration.
    /// </summary>
    public async Task<OrchestrationResult> AddEntityWithMigrationAsync(
        string entityName,
        string? entityNamespace = null,
        bool createMigration = true,
        bool applyMigration = true)
    {
        var steps = new List<string>();
        
        try
        {
            // Step 1: Add DbSet to DbContext
            LogStep("Adding DbSet to DbContext...");
            var modifyResult = DbContextModifier.AddDbSetToContext(
                _projectDirectory, 
                entityName, 
                entityNamespace);

            if (!modifyResult.IsSuccess)
            {
                return OrchestrationResult.Failure(
                    modifyResult.Message,
                    steps);
            }

            steps.Add($"✅ Added DbSet<{entityName}> to {Path.GetFileName(modifyResult.FilePath!)}");

            if (!createMigration)
            {
                return OrchestrationResult.Success(
                    $"Added DbSet<{entityName}> to DbContext (migration skipped)",
                    steps);
            }

            // Step 2: Create EF Core migration
            var migrationName = $"Add{entityName}";
            LogStep($"Creating migration: {migrationName}...");

            var createResult = await CreateMigrationAsync(migrationName);
            if (!createResult.IsSuccess)
            {
                // Rollback: Remove DbSet from DbContext
                await RollbackDbSetAsync(entityName, modifyResult.FilePath!);
                return OrchestrationResult.Failure(
                    $"Failed to create migration: {createResult.Error}\nDbSet addition rolled back.",
                    steps);
            }

            steps.Add($"✅ Created migration: {migrationName}");

            if (!applyMigration)
            {
                return OrchestrationResult.Success(
                    $"Added {entityName} with migration (database update skipped)",
                    steps);
            }

            // Step 3: Apply migration to database
            LogStep("Applying migration to database...");

            var updateResult = await UpdateDatabaseAsync();
            if (!updateResult.IsSuccess)
            {
                // Rollback: Remove migration and DbSet
                await RollbackMigrationAsync(migrationName);
                await RollbackDbSetAsync(entityName, modifyResult.FilePath!);
                return OrchestrationResult.Failure(
                    $"Failed to update database: {updateResult.Error}\nChanges rolled back.",
                    steps);
            }

            steps.Add("✅ Applied migration to database");

            return OrchestrationResult.Success(
                $"Successfully added {entityName} with migration {migrationName}",
                steps);
        }
        catch (Exception ex)
        {
            return OrchestrationResult.Failure(
                $"Unexpected error: {ex.Message}",
                steps);
        }
    }

    /// <summary>
    /// Creates an EF Core migration (public wrapper for CLI commands).
    /// </summary>
    public async Task<OrchestrationResult> CreateMigrationOnlyAsync(string migrationName)
    {
        var steps = new List<string>();
        
        try
        {
            LogStep($"Creating migration: {migrationName}...");
            var result = await CreateMigrationAsync(migrationName);
            
            if (result.IsSuccess)
            {
                steps.Add($"✅ Created migration: {migrationName}");
                return OrchestrationResult.Success($"Migration {migrationName} created successfully", steps);
            }
            else
            {
                return OrchestrationResult.Failure($"Failed to create migration: {result.Error}", steps);
            }
        }
        catch (Exception ex)
        {
            return OrchestrationResult.Failure($"Error: {ex.Message}", steps);
        }
    }

    /// <summary>
    /// Applies pending migrations to the database (public wrapper for CLI commands).
    /// </summary>
    public async Task<OrchestrationResult> UpdateDatabaseOnlyAsync()
    {
        var steps = new List<string>();
        
        try
        {
            LogStep("Applying migrations to database...");
            var result = await UpdateDatabaseAsync();
            
            if (result.IsSuccess)
            {
                steps.Add("✅ Applied all pending migrations");
                return OrchestrationResult.Success("Database updated successfully", steps);
            }
            else
            {
                return OrchestrationResult.Failure($"Failed to update database: {result.Error}", steps);
            }
        }
        catch (Exception ex)
        {
            return OrchestrationResult.Failure($"Error: {ex.Message}", steps);
        }
    }

    /// <summary>
    /// Rolls back the last migration (public wrapper for CLI commands).
    /// </summary>
    public async Task<OrchestrationResult> RollbackMigrationOnlyAsync()
    {
        var steps = new List<string>();
        
        try
        {
            LogStep("Rolling back last migration...");
            var result = await RollbackMigrationAsync("last");
            
            if (result.IsSuccess)
            {
                steps.Add("✅ Removed last migration");
                steps.Add("✅ Database rolled back");
                return OrchestrationResult.Success("Migration rolled back successfully", steps);
            }
            else
            {
                return OrchestrationResult.Failure($"Failed to rollback: {result.Error}", steps);
            }
        }
        catch (Exception ex)
        {
            return OrchestrationResult.Failure($"Error: {ex.Message}", steps);
        }
    }

    /// <summary>
    /// Creates an EF Core migration.
    /// </summary>
    private async Task<CommandResult> CreateMigrationAsync(string migrationName)
    {
        return await RunDotnetEfCommandAsync($"migrations add {migrationName}");
    }

    /// <summary>
    /// Applies pending migrations to the database.
    /// </summary>
    private async Task<CommandResult> UpdateDatabaseAsync()
    {
        return await RunDotnetEfCommandAsync("database update");
    }

    /// <summary>
    /// Removes the last migration (rollback).
    /// </summary>
    private async Task<CommandResult> RollbackMigrationAsync(string migrationName)
    {
        LogStep($"Rolling back migration: {migrationName}...");
        return await RunDotnetEfCommandAsync("migrations remove");
    }

    /// <summary>
    /// Removes a DbSet from the DbContext (rollback).
    /// </summary>
    private async Task<bool> RollbackDbSetAsync(string entityName, string dbContextPath)
    {
        try
        {
            LogStep($"Removing DbSet<{entityName}> from DbContext...");
            
            var code = await File.ReadAllTextAsync(dbContextPath);
            
            // Simple approach: Remove the DbSet line
            var propertyName = GetPluralName(entityName);
            var lines = code.Split('\n')
                .Where(line => !line.Contains($"DbSet<{entityName}>") && 
                               !line.Contains($"{propertyName}"))
                .ToList();
            
            var modifiedCode = string.Join('\n', lines);
            await File.WriteAllTextAsync(dbContextPath, modifiedCode);
            
            LogStep($"✅ Removed DbSet<{entityName}>");
            return true;
        }
        catch (Exception ex)
        {
            LogStep($"⚠️ Failed to rollback DbSet: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Runs a dotnet ef command.
    /// </summary>
    private async Task<CommandResult> RunDotnetEfCommandAsync(string arguments)
    {
        try
        {
            // Check if dotnet-ef is installed
            if (!await IsEfCoreToolInstalledAsync())
            {
                return CommandResult.Failure(
                    "EF Core tools not installed. Run: dotnet tool install --global dotnet-ef");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"ef {arguments}",
                WorkingDirectory = _projectDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            
            var output = new StringBuilder();
            var error = new StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    output.AppendLine(e.Data);
                    if (_verbose) Console.WriteLine(e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    error.AppendLine(e.Data);
                    if (_verbose) Console.Error.WriteLine(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var errorMessage = error.Length > 0 ? error.ToString() : output.ToString();
                return CommandResult.Failure(errorMessage.Trim());
            }

            return CommandResult.Success(output.ToString().Trim());
        }
        catch (Exception ex)
        {
            return CommandResult.Failure($"Failed to run dotnet ef: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if dotnet-ef tool is installed.
    /// </summary>
    private async Task<bool> IsEfCoreToolInstalledAsync()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "tool list --global",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) return false;

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return output.Contains("dotnet-ef");
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Simple pluralization (matches CodeModificationHelper logic).
    /// </summary>
    private string GetPluralName(string entityName)
    {
        if (entityName.EndsWith("s", StringComparison.OrdinalIgnoreCase) ||
            entityName.EndsWith("x", StringComparison.OrdinalIgnoreCase) ||
            entityName.EndsWith("ch", StringComparison.OrdinalIgnoreCase) ||
            entityName.EndsWith("sh", StringComparison.OrdinalIgnoreCase))
        {
            return entityName + "es";
        }
        
        if (entityName.EndsWith("y", StringComparison.OrdinalIgnoreCase))
        {
            return entityName.Substring(0, entityName.Length - 1) + "ies";
        }

        return entityName + "s";
    }

    private void LogStep(string message)
    {
        if (_verbose)
        {
            Console.WriteLine($"  {message}");
        }
    }
}

/// <summary>
/// Result of the orchestration operation.
/// </summary>
public class OrchestrationResult
{
    public bool IsSuccess { get; private set; }
    public string Message { get; private set; }
    public List<string> Steps { get; private set; }

    private OrchestrationResult(bool isSuccess, string message, List<string> steps)
    {
        IsSuccess = isSuccess;
        Message = message;
        Steps = steps;
    }

    public static OrchestrationResult Success(string message, List<string> steps)
        => new OrchestrationResult(true, message, steps);

    public static OrchestrationResult Failure(string message, List<string> steps)
        => new OrchestrationResult(false, message, steps);
}

/// <summary>
/// Result of a command execution.
/// </summary>
internal class CommandResult
{
    public bool IsSuccess { get; private set; }
    public string Output { get; private set; }
    public string Error { get; private set; }

    private CommandResult(bool isSuccess, string output, string error)
    {
        IsSuccess = isSuccess;
        Output = output;
        Error = error;
    }

    public static CommandResult Success(string output)
        => new CommandResult(true, output, string.Empty);

    public static CommandResult Failure(string error)
        => new CommandResult(false, string.Empty, error);
}

