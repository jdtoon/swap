using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Swap.CLI.Infrastructure;

/// <summary>
/// Uses Roslyn to inject DbSet properties into DbContext files.
/// DEPRECATED: Use DbContextModifier instead for better error handling and features.
/// </summary>
[Obsolete("Use DbContextModifier.AddDbSetToContext instead. This class will be removed in a future version.")]
public class DbContextInjector
{
    /// <summary>
    /// Adds a DbSet property to the DbContext class.
    /// </summary>
    /// <param name="dbContextPath">Path to the DbContext.cs file</param>
    /// <param name="entityName">Name of the entity (e.g., "Product")</param>
    /// <returns>True if successful, false otherwise</returns>
    public static async Task<bool> AddDbSetAsync(string dbContextPath, string entityName)
    {
        // Delegate to new implementation
        try
        {
            var code = await File.ReadAllTextAsync(dbContextPath);
            var modifiedCode = CodeModificationHelper.AddDbSetProperty(code, entityName, null);
            await File.WriteAllTextAsync(dbContextPath, modifiedCode);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to add DbSet: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Finds the DbContext file in the current solution.
    /// </summary>
    public static string? FindDbContext()
    {
        var currentDir = Directory.GetCurrentDirectory();
        return CodeModificationHelper.FindDbContextFile(currentDir);
    }
}

