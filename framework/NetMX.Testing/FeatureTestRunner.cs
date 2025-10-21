using System.Diagnostics;
using System.Text;

namespace NetMX.Testing;

/// <summary>
/// Runs CLI commands and verifies results for feature testing.
/// Enables testing features in isolation with SQLite databases.
/// </summary>
public class FeatureTestRunner : IDisposable
{
    private readonly string _projectPath;
    private readonly bool _cleanupOnDispose;

    /// <summary>
    /// Gets the path to the test project directory.
    /// </summary>
    public string ProjectPath => _projectPath;

    /// <summary>
    /// Creates a new feature test runner with a temporary project.
    /// </summary>
    /// <param name="projectName">Name of the test project (default: random GUID)</param>
    /// <param name="cleanupOnDispose">Whether to delete project directory on disposal (default: true)</param>
    public FeatureTestRunner(string? projectName = null, bool cleanupOnDispose = true)
    {
        _projectPath = TestProjectFactory.CreateTestProject(projectName);
        _cleanupOnDispose = cleanupOnDispose;
    }

    /// <summary>
    /// Runs a CLI command in the test project directory.
    /// </summary>
    /// <param name="command">CLI command to run (e.g., "generate feature Product")</param>
    /// <param name="timeout">Command timeout in seconds (default: 30)</param>
    /// <returns>Result containing exit code, stdout, and stderr</returns>
    public async Task<CliCommandResult> RunCliCommandAsync(string command, int timeout = 30)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "netmx",
            Arguments = command,
            WorkingDirectory = _projectPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = new Process { StartInfo = processStartInfo };
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                outputBuilder.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                errorBuilder.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeout));
        var processTask = Task.Run(() => process.WaitForExit());

        var completedTask = await Task.WhenAny(processTask, timeoutTask);

        if (completedTask == timeoutTask)
        {
            try
            {
                process.Kill();
            }
            catch
            {
                // Ignore if already exited
            }

            throw new TimeoutException($"CLI command timed out after {timeout} seconds: netmx {command}");
        }

        return new CliCommandResult
        {
            ExitCode = process.ExitCode,
            Output = outputBuilder.ToString(),
            Error = errorBuilder.ToString(),
            Success = process.ExitCode == 0
        };
    }

    /// <summary>
    /// Verifies that a file exists in the test project.
    /// </summary>
    /// <param name="relativePath">Path relative to project root</param>
    /// <returns>True if file exists</returns>
    public bool FileExists(string relativePath)
    {
        var fullPath = Path.Combine(_projectPath, relativePath);
        return File.Exists(fullPath);
    }

    /// <summary>
    /// Reads the content of a file in the test project.
    /// </summary>
    /// <param name="relativePath">Path relative to project root</param>
    /// <returns>File content</returns>
    public string ReadFile(string relativePath)
    {
        var fullPath = Path.Combine(_projectPath, relativePath);
        return File.ReadAllText(fullPath);
    }

    /// <summary>
    /// Verifies that a file contains the specified text.
    /// </summary>
    /// <param name="relativePath">Path relative to project root</param>
    /// <param name="expectedText">Text that should be present</param>
    /// <returns>True if file contains the text</returns>
    public bool FileContains(string relativePath, string expectedText)
    {
        if (!FileExists(relativePath))
            return false;

        var content = ReadFile(relativePath);
        return content.Contains(expectedText, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets all files in a directory relative to project root.
    /// </summary>
    /// <param name="relativePath">Directory path relative to project root</param>
    /// <param name="searchPattern">File search pattern (default: *.*)</param>
    /// <returns>Array of file paths relative to project root</returns>
    public string[] GetFiles(string relativePath, string searchPattern = "*.*")
    {
        var fullPath = Path.Combine(_projectPath, relativePath);
        if (!Directory.Exists(fullPath))
            return Array.Empty<string>();

        var files = Directory.GetFiles(fullPath, searchPattern);
        return files.Select(f => Path.GetRelativePath(_projectPath, f)).ToArray();
    }

    /// <summary>
    /// Verifies that the database was created successfully.
    /// </summary>
    /// <returns>True if database file exists</returns>
    public bool DatabaseExists()
    {
        var dbFiles = GetFiles("Data", "*.db");
        return dbFiles.Length > 0;
    }

    /// <summary>
    /// Gets the count of migration files in the project.
    /// </summary>
    /// <returns>Number of migration files</returns>
    public int GetMigrationCount()
    {
        var migrationFiles = GetFiles("Migrations", "*.cs")
            .Where(f => !f.EndsWith("ModelSnapshot.cs", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        return migrationFiles.Length;
    }

    /// <summary>
    /// Cleans up the test project directory.
    /// </summary>
    public void Dispose()
    {
        if (_cleanupOnDispose)
        {
            TestProjectFactory.CleanupTestProject(_projectPath);
        }
    }
}

/// <summary>
/// Result of running a CLI command.
/// </summary>
public class CliCommandResult
{
    /// <summary>
    /// Exit code of the command.
    /// </summary>
    public int ExitCode { get; init; }

    /// <summary>
    /// Standard output from the command.
    /// </summary>
    public string Output { get; init; } = string.Empty;

    /// <summary>
    /// Standard error from the command.
    /// </summary>
    public string Error { get; init; } = string.Empty;

    /// <summary>
    /// Whether the command succeeded (exit code 0).
    /// </summary>
    public bool Success { get; init; }
}
