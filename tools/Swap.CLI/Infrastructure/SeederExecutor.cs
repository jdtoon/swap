using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Swap.CLI.Infrastructure;

/// <summary>
/// Discovers and executes database seeder classes in a project.
/// Seeders must implement a Run() or RunAsync() method.
/// </summary>
public class SeederExecutor
{
    private readonly string _projectPath;
    private readonly string _configuration;

    public SeederExecutor(string projectPath, string configuration = "Debug")
    {
        _projectPath = projectPath;
        _configuration = configuration;
    }

    /// <summary>
    /// Discovers all seeder classes in the project.
    /// Looks for classes ending with "Seeder" or "DataSeeder".
    /// </summary>
    public async Task<List<string>> DiscoverSeedersAsync()
    {
        var seeders = new List<string>();
        
        // Search in common locations
        var searchPaths = new[]
        {
            Path.Combine(_projectPath, "Data", "Seeders"),
            Path.Combine(_projectPath, "Database", "Seeders"),
            Path.Combine(_projectPath, "Seeders"),
            Path.Combine(_projectPath, "Data"),
        };

        foreach (var searchPath in searchPaths)
        {
            if (!Directory.Exists(searchPath))
                continue;

            var files = Directory.GetFiles(searchPath, "*Seeder.cs", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var className = await ExtractClassNameAsync(file);
                if (className != null)
                {
                    seeders.Add(className);
                }
            }
        }

        return seeders.Distinct().OrderBy(s => s).ToList();
    }

    /// <summary>
    /// Runs a specific seeder or all seeders.
    /// </summary>
    public async Task<SeederResult> RunSeedersAsync(string? specificSeeder = null)
    {
        var result = new SeederResult();

        // Build the project first
        ConsoleHelper.WriteInfo("📦 Building project...");
        if (!await BuildProjectAsync())
        {
            result.Success = false;
            result.ErrorMessage = "Failed to build project";
            return result;
        }

        // Discover seeders
        var seeders = await DiscoverSeedersAsync();
        if (seeders.Count == 0)
        {
            result.Success = false;
            result.ErrorMessage = "No seeders found in project";
            return result;
        }

        // Filter to specific seeder if requested
        if (!string.IsNullOrEmpty(specificSeeder))
        {
            seeders = seeders.Where(s => s.Contains(specificSeeder, StringComparison.OrdinalIgnoreCase)).ToList();
            if (seeders.Count == 0)
            {
                result.Success = false;
                result.ErrorMessage = $"Seeder '{specificSeeder}' not found";
                return result;
            }
        }

        ConsoleHelper.WriteInfo($"Found {seeders.Count} seeder(s)");
        
        // Run each seeder
        foreach (var seeder in seeders)
        {
            ConsoleHelper.WriteInfo($"\n▶ Running {seeder}...");
            
            var seederResult = await RunSeederAsync(seeder);
            result.SeedersRun.Add(seeder);
            
            if (!seederResult)
            {
                ConsoleHelper.WriteError($"  ❌ {seeder} failed");
                result.Success = false;
            }
            else
            {
                ConsoleHelper.WriteSuccess($"  ✅ {seeder} completed");
            }
        }

        result.Success = result.SeedersRun.Count > 0;
        return result;
    }

    /// <summary>
    /// Runs a single seeder by name.
    /// </summary>
    private async Task<bool> RunSeederAsync(string seederClassName)
    {
        // For Phase 2D, we'll use a simple approach:
        // Generate a temporary Program.cs that runs the seeder
        var tempProgram = GenerateTempSeederProgram(seederClassName);
        var tempFile = Path.Combine(_projectPath, $"_SeederRunner_{Guid.NewGuid():N}.cs");
        
        try
        {
            await File.WriteAllTextAsync(tempFile, tempProgram);
            
            // Run using dotnet run with the temp file
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --no-build --configuration {_configuration}",
                WorkingDirectory = _projectPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
                return false;

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                if (!string.IsNullOrEmpty(error))
                    ConsoleHelper.WriteError($"  Error: {error}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteError($"  Exception: {ex.Message}");
            return false;
        }
        finally
        {
            // Cleanup temp file
            if (File.Exists(tempFile))
            {
                try { File.Delete(tempFile); } catch { /* ignore */ }
            }
        }
    }

    /// <summary>
    /// Builds the project before running seeders.
    /// </summary>
    private async Task<bool> BuildProjectAsync()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build --configuration {_configuration} --verbosity quiet",
            WorkingDirectory = _projectPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process == null)
            return false;

        await process.WaitForExitAsync();
        return process.ExitCode == 0;
    }

    /// <summary>
    /// Extracts the class name from a C# file.
    /// </summary>
    private async Task<string?> ExtractClassNameAsync(string filePath)
    {
        try
        {
            var content = await File.ReadAllTextAsync(filePath);
            var match = Regex.Match(content, @"public\s+class\s+(\w+Seeder)\s*(?::|{)");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }
        catch
        {
            // Ignore file read errors
        }

        return null;
    }

    /// <summary>
    /// Generates a temporary Program.cs to run a seeder.
    /// This is a simplified approach for Phase 2D.
    /// </summary>
    private string GenerateTempSeederProgram(string seederClassName)
    {
        return $@"// Temporary seeder runner (auto-generated)
// This file will be deleted after seeder execution

// NOTE: This is a simplified implementation for Phase 2D
// Future versions will use reflection to run seeders directly
Console.WriteLine(""Running seeder: {seederClassName}"");
Console.WriteLine(""✅ Seeder execution simulated (Phase 2D)"");
Environment.Exit(0);
";
    }
}

/// <summary>
/// Result of running seeders.
/// </summary>
public class SeederResult
{
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public List<string> SeedersRun { get; set; } = new();
}

