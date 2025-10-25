namespace Swap.CLI.Models;

/// <summary>
/// Options for seeder generation.
/// </summary>
public class SeederGenerationOptions
{
    /// <summary>
    /// Seeder class name (e.g., "ProductSeeder")
    /// </summary>
    public string SeederName { get; set; } = string.Empty;

    /// <summary>
    /// Entity name (e.g., "Product")
    /// </summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// Namespace for the seeder (e.g., "MyApp.Web.Seeding" or "Authorization.Application.Seeding")
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Module name (optional, e.g., "Authorization")
    /// If null, generates in app context
    /// </summary>
    public string? ModuleName { get; set; }

    /// <summary>
    /// Key type for the entity (default: Guid)
    /// </summary>
    public string KeyType { get; set; } = "Guid";
}

