namespace Swap.Patterns.Versionable;

/// <summary>
/// Marks an entity as versionable with an integer version counter.
/// The Version value is automatically initialized and incremented by the VersionInterceptor.
/// </summary>
public interface IVersionable
{
    /// <summary>
    /// The current version number of the entity. Starts at 1 and increments on each update.
    /// </summary>
    int Version { get; set; }
}
