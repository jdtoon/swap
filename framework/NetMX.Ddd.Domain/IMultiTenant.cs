namespace NetMX.Ddd.Domain;

/// <summary>
/// Interface for entities that belong to a specific tenant in a multi-tenant application.
/// </summary>
public interface IMultiTenant
{
    /// <summary>
    /// Gets the unique identifier of the tenant this entity belongs to.
    /// </summary>
    Guid TenantId { get; }
}