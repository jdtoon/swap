using System.Reflection;
using PublicApiGenerator;
using Xunit;

namespace Swap.Htmx.Tests;

public class PublicApiSnapshotTests
{
    [Fact]
    public void PublicApi_DoesNotChange_Accidentally()
    {
        // Note: this test is intentionally strict. If you intentionally change the public API,
        // update the snapshot files under PublicApiSnapshots/.

        var repoRoot = FindRepoRoot();
        var snapshotDir = Path.Combine(repoRoot, "lib", "Swap.Htmx.Tests", "PublicApiSnapshots");
        Directory.CreateDirectory(snapshotDir);

        AssertSnapshot(snapshotDir, "Swap.Htmx", typeof(Swap.Htmx.Models.SwapResponseBuilder).Assembly);
        AssertSnapshot(snapshotDir, "Swap.Htmx.Realtime", typeof(Swap.Htmx.Realtime.RealtimeEventMiddleware).Assembly);
        AssertSnapshot(snapshotDir, "Swap.Htmx.Realtime.Redis", typeof(Swap.Htmx.Realtime.Redis.RedisSseBackplane).Assembly);
    }

    private static void AssertSnapshot(string snapshotDir, string name, Assembly assembly)
    {
        var actual = ApiGenerator.GeneratePublicApi(assembly);
        actual = NormalizeNewlines(actual);

        var snapshotPath = Path.Combine(snapshotDir, name + ".PublicApi.txt");
        var actualPath = Path.Combine(snapshotDir, name + ".PublicApi.actual.txt");

        if (!File.Exists(snapshotPath))
        {
            File.WriteAllText(actualPath, actual);
            Assert.Fail($"Missing API snapshot '{snapshotPath}'. Generated '{actualPath}'. Commit an approved snapshot to enable this gate.");
        }

        var expected = NormalizeNewlines(File.ReadAllText(snapshotPath));
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            File.WriteAllText(actualPath, actual);
            Assert.Fail($"Public API changed for {name}. Review '{actualPath}' and update '{snapshotPath}' if intentional.");
        }

        if (File.Exists(actualPath))
        {
            // Clean up stale outputs when the snapshot matches.
            File.Delete(actualPath);
        }
    }

    private static string NormalizeNewlines(string text)
        => text.Replace("\r\n", "\n");

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "swap.sln")))
                return current.FullName;

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repo root (swap.sln not found walking upward from current directory).");
    }
}
