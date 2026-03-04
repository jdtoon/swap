using System.Text.RegularExpressions;
using Xunit;

namespace Swap.Htmx.Tests;

public class ClientAssetVersionDriftTests
{
    [Fact]
    public void ClientAssetVersions_DoNot_Drift_From_RecommendedVersionsDoc()
    {
        var repoRoot = FindRepoRoot();

        var recommendedVersionsPath = Path.Combine(repoRoot, "lib", "Swap.Htmx", "docs", "RecommendedVersions.md");
        Assert.True(File.Exists(recommendedVersionsPath), $"Expected {recommendedVersionsPath} to exist.");

        var recommended = ParseRecommendedVersions(File.ReadAllText(recommendedVersionsPath));

        var filesToScan = GetFilesToScan(repoRoot).ToArray();
        Assert.NotEmpty(filesToScan);

        var mismatches = new List<string>();
        foreach (var file in filesToScan)
        {
            var text = File.ReadAllText(file);

            AddMismatches(mismatches, repoRoot, file, text, "htmx.org", recommended.Htmx);
            AddMismatches(mismatches, repoRoot, file, text, "htmx-ext-sse", recommended.Sse);
            AddMismatches(mismatches, repoRoot, file, text, "htmx-ext-ws", recommended.Ws);
        }

        if (mismatches.Count > 0)
        {
            var message =
                "Client asset version drift detected.\n" +
                $"Expected (from docs/RecommendedVersions.md): htmx={recommended.Htmx}, htmx-ext-sse={recommended.Sse}, htmx-ext-ws={recommended.Ws}\n\n" +
                string.Join("\n", mismatches);

            Assert.Fail(message);
        }
    }

    private static void AddMismatches(List<string> mismatches, string repoRoot, string filePath, string text, string libraryName, string expectedVersion)
    {
        // Matches:
        // - htmx.org@2.0.8
        // - https://unpkg.com/htmx.org@2.0.8
        // - "library": "htmx.org@2.0.8"
        var regex = new Regex($@"{Regex.Escape(libraryName)}@(?<ver>\d+(?:\.\d+)+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        foreach (Match match in regex.Matches(text))
        {
            var found = match.Groups["ver"].Value;
            if (!string.Equals(found, expectedVersion, StringComparison.OrdinalIgnoreCase))
            {
                mismatches.Add($"- {Path.GetRelativePath(repoRoot, filePath)}: {libraryName}@{found} (expected {libraryName}@{expectedVersion})");
            }
        }
    }

    private static IEnumerable<string> GetFilesToScan(string repoRoot)
    {
        // Keep the scan targeted to reduce false positives and keep this test fast.
        foreach (var file in Directory.EnumerateFiles(Path.Combine(repoRoot, "templates"), "libman*.json", SearchOption.AllDirectories))
        {
            if (IsBuildArtifactPath(file))
                continue;

            yield return file;
        }

        foreach (var file in Directory.EnumerateFiles(Path.Combine(repoRoot, "demo"), "libman.json", SearchOption.AllDirectories))
        {
            if (IsBuildArtifactPath(file))
                continue;

            yield return file;
        }

        yield return Path.Combine(repoRoot, "llms.txt");
        yield return Path.Combine(repoRoot, "lib", "Swap.Htmx", "README.md");

        foreach (var file in Directory.EnumerateFiles(Path.Combine(repoRoot, "lib", "Swap.Htmx", "docs"), "*.md", SearchOption.AllDirectories))
            yield return file;

        // A few demos use CDN script tags in views/layouts.
        foreach (var file in Directory.EnumerateFiles(Path.Combine(repoRoot, "demo"), "*.cshtml", SearchOption.AllDirectories))
        {
            if (IsBuildArtifactPath(file))
                continue;

            yield return file;
        }
    }

    private static bool IsBuildArtifactPath(string fullPath)
    {
        return fullPath.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
               fullPath.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static RecommendedVersions ParseRecommendedVersions(string markdown)
    {
        static string RequireVersion(string markdownText, string key)
        {
            // Example line: - **htmx**: `2.0.8`
            var match = Regex.Match(
                markdownText,
                $@"\*\*{Regex.Escape(key)}\*\*.*?:\s*`(?<ver>[^`]+)`",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            if (!match.Success)
                throw new InvalidOperationException($"Could not parse '{key}' version from RecommendedVersions.md");

            return match.Groups["ver"].Value.Trim();
        }

        return new RecommendedVersions(
            Htmx: RequireVersion(markdown, "htmx"),
            Sse: RequireVersion(markdown, "htmx SSE extension"),
            Ws: RequireVersion(markdown, "htmx WebSocket extension"));
    }

    private static string FindRepoRoot()
    {
        // Test runner cwd can vary; walk upward until we find swap.sln.
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current != null)
        {
            var sln = Path.Combine(current.FullName, "swap.sln");
            if (File.Exists(sln))
                return current.FullName;

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repo root (swap.sln not found walking upward from current directory).");
    }

    private sealed record RecommendedVersions(string Htmx, string Sse, string Ws);
}
