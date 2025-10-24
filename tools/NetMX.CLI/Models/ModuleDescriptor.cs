using System.Text.Json;

namespace NetMX.CLI.Models;

/// <summary>
/// Represents a NetMX module descriptor (module.json)
/// </summary>
public class ModuleDescriptor
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public List<string> Dependencies { get; set; } = new();
    public List<ModuleProject> Projects { get; set; } = new();
    public List<ModuleService>? Services { get; set; }
    public ModuleMigrations? Migrations { get; set; }
    public Dictionary<string, object>? Configuration { get; set; }
    public List<ModuleFeature>? Features { get; set; }
    public List<ModuleRoute>? Routes { get; set; }
    public List<ModuleMiddleware>? Middleware { get; set; }

    public static ModuleDescriptor? LoadFrom(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<ModuleDescriptor>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
}

public class ModuleProject
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

public class ModuleService
{
    public string Type { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public string? ConnectionStringName { get; set; }
    public string? MigrationHistoryTable { get; set; }
    public string? Description { get; set; }
}

public class ModuleMigrations
{
    public bool Enabled { get; set; }
    public bool AutoApply { get; set; }
    public string? ContextName { get; set; }
    public string? MigrationHistoryTable { get; set; }
}

public class ModuleFeature
{
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string? Description { get; set; }
}

public class ModuleRoute
{
    public string Pattern { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class ModuleMiddleware
{
    public string Type { get; set; } = string.Empty;
    public int Order { get; set; }
    public string? Description { get; set; }
}

