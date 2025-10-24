namespace NetMX.Ddd.Domain;

/// <summary>
/// Interface for entities that support optimistic concurrency control via a concurrency stamp.
/// </summary>
public interface IHasConcurrencyStamp
{
    /// <summary>
    /// Gets or sets the concurrency stamp used for optimistic concurrency checks.
    /// </summary>
    string ConcurrencyStamp { get; set; }
}