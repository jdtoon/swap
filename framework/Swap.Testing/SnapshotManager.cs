namespace Swap.Testing;

/// <summary>
/// Provides snapshot testing capabilities for HTML responses.
/// </summary>
public static class SnapshotManager
{
    private static readonly string DefaultSnapshotDirectory = "__snapshots__";

    /// <summary>
    /// Compare HTML content against a saved snapshot.
    /// </summary>
    /// <param name="snapshotName">Name of the snapshot (without extension)</param>
    /// <param name="actualContent">The actual HTML content to compare</param>
    /// <param name="snapshotDirectory">Directory to store snapshots (default: __snapshots__)</param>
    /// <param name="updateSnapshots">If true, update the snapshot instead of comparing</param>
    /// <returns>True if content matches snapshot, false otherwise</returns>
    public static async Task<bool> MatchesSnapshotAsync(
        string snapshotName,
        string actualContent,
        string? snapshotDirectory = null,
        bool updateSnapshots = false)
    {
        var snapshotDir = snapshotDirectory ?? DefaultSnapshotDirectory;
        Directory.CreateDirectory(snapshotDir);

        var snapshotPath = Path.Combine(snapshotDir, $"{snapshotName}.html");
        var normalizedActual = NormalizeHtml(actualContent);

        // Update mode - save snapshot and return true
        if (updateSnapshots || !File.Exists(snapshotPath))
        {
            await File.WriteAllTextAsync(snapshotPath, normalizedActual);
            return true;
        }

        // Compare mode
        var expectedContent = await File.ReadAllTextAsync(snapshotPath);
        var normalizedExpected = NormalizeHtml(expectedContent);

        if (normalizedActual == normalizedExpected)
            return true;

        // Content mismatch - create a diff file
        var diffPath = Path.Combine(snapshotDir, $"{snapshotName}.diff.html");
        await File.WriteAllTextAsync(diffPath, actualContent);

        return false;
    }

    /// <summary>
    /// Assert that content matches snapshot, throw if not.
    /// </summary>
    public static async Task AssertMatchesSnapshotAsync(
        string snapshotName,
        string actualContent,
        string? snapshotDirectory = null,
        bool updateSnapshots = false)
    {
        var matches = await MatchesSnapshotAsync(snapshotName, actualContent, snapshotDirectory, updateSnapshots);

        if (!matches)
        {
            var snapshotDir = snapshotDirectory ?? DefaultSnapshotDirectory;
            var diffPath = Path.Combine(snapshotDir, $"{snapshotName}.diff.html");
            
            throw new HtmxTestException(
                $"Snapshot mismatch for '{snapshotName}'.\n" +
                $"Expected snapshot: {snapshotDir}/{snapshotName}.html\n" +
                $"Actual content saved to: {diffPath}\n" +
                $"To update snapshot, set UPDATE_SNAPSHOTS=true environment variable or pass updateSnapshots: true");
        }
    }

    /// <summary>
    /// Delete a snapshot file.
    /// </summary>
    public static void DeleteSnapshot(string snapshotName, string? snapshotDirectory = null)
    {
        var snapshotDir = snapshotDirectory ?? DefaultSnapshotDirectory;
        var snapshotPath = Path.Combine(snapshotDir, $"{snapshotName}.html");
        
        if (File.Exists(snapshotPath))
            File.Delete(snapshotPath);
    }

    /// <summary>
    /// Check if update mode is enabled via environment variable.
    /// </summary>
    public static bool IsUpdateMode()
    {
        var updateEnv = Environment.GetEnvironmentVariable("UPDATE_SNAPSHOTS");
        return !string.IsNullOrEmpty(updateEnv) && 
               (updateEnv.Equals("true", StringComparison.OrdinalIgnoreCase) || updateEnv == "1");
    }

    /// <summary>
    /// Normalize HTML for consistent comparison (trim whitespace, normalize line endings).
    /// </summary>
    private static string NormalizeHtml(string html)
    {
        // Normalize line endings
        html = html.Replace("\r\n", "\n").Replace("\r", "\n");

        // Trim each line and remove empty lines
        var lines = html.Split('\n')
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line));

        return string.Join('\n', lines);
    }
}
