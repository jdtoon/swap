using System.Text.Json;
using System.Text.Json.Serialization;

namespace Swap.CLI.Infrastructure;

public class SwapConfig
{
    [JsonPropertyName("version")] public int Version { get; set; } = 1;
    [JsonPropertyName("entities")] public Dictionary<string, EntityConfig> Entities { get; set; } = new();
}

public class EntityConfig
{
    [JsonPropertyName("patterns")] public HashSet<string> Patterns { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    [JsonPropertyName("wiring")] public Dictionary<string, bool> Wiring { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    [JsonPropertyName("lastApplied")] public DateTimeOffset LastApplied { get; set; } = DateTimeOffset.UtcNow;
}

public static class SwapConfigManager
{
    private const string FileName = "swap-config.json";

    public static (SwapConfig Config, string Path) LoadOrCreate(string workingDir)
    {
        var path = System.IO.Path.Combine(workingDir, FileName);
        if (!System.IO.File.Exists(path))
        {
            return (new SwapConfig(), path);
        }
        try
        {
            var json = System.IO.File.ReadAllText(path);
            var cfg = JsonSerializer.Deserialize<SwapConfig>(json, JsonOptions()) ?? new SwapConfig();
            return (cfg, path);
        }
        catch
        {
            // Defensive: if corrupted, start fresh but don't overwrite yet
            return (new SwapConfig(), path);
        }
    }

    public static void Save(SwapConfig config, string path)
    {
        var json = JsonSerializer.Serialize(config, JsonOptions());
        System.IO.File.WriteAllText(path, json);
    }

    public static void RecordPattern(SwapConfig config, string entity, string pattern, IDictionary<string, bool>? wiring = null)
    {
        if (!config.Entities.TryGetValue(entity, out var ec))
        {
            ec = new EntityConfig();
            config.Entities[entity] = ec;
        }
        ec.Patterns.Add(pattern);
        if (wiring != null)
        {
            foreach (var kv in wiring)
            {
                ec.Wiring[kv.Key] = kv.Value;
            }
        }
        ec.LastApplied = DateTimeOffset.UtcNow;
    }

    public static void RemovePattern(SwapConfig config, string entity, string pattern)
    {
        if (config.Entities.TryGetValue(entity, out var ec))
        {
            ec.Patterns.Remove(pattern);
            ec.LastApplied = DateTimeOffset.UtcNow;
            if (ec.Patterns.Count == 0)
            {
                // Clean up empty entity entries
                config.Entities.Remove(entity);
            }
        }
    }

    public static bool IsPatternInUse(SwapConfig config, string pattern)
    {
        return config.Entities.Values.Any(e => e.Patterns.Contains(pattern));
    }

    private static JsonSerializerOptions JsonOptions() => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
