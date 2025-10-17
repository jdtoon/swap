using NetMX.Ddd.Domain.Entities;

namespace NetMX.Identity.Core.Tenants;

/// <summary>
/// Represents a tenant in a multi-tenant system.
/// </summary>
public class Tenant : AggregateRoot<Guid>
{
    /// <summary>
    /// The unique tenant name/identifier.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// The normalized tenant name for lookups.
    /// </summary>
    public string NormalizedName { get; private set; }

    /// <summary>
    /// The tenant display name.
    /// </summary>
    public string DisplayName { get; private set; }

    /// <summary>
    /// Whether the tenant is active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// The tenant's subscription start date.
    /// </summary>
    public DateTime? SubscriptionStartDate { get; private set; }

    /// <summary>
    /// The tenant's subscription end date.
    /// </summary>
    public DateTime? SubscriptionEndDate { get; private set; }

    /// <summary>
    /// Additional tenant metadata as JSON.
    /// </summary>
    public string? Metadata { get; private set; }

    // EF Core constructor
    private Tenant()
    {
        Name = string.Empty;
        NormalizedName = string.Empty;
        DisplayName = string.Empty;
    }

    /// <summary>
    /// Creates a new tenant.
    /// </summary>
    public Tenant(
        Guid id,
        string name,
        string displayName)
    {
        Id = id;
        Name = Guard.NotNullOrEmpty(name, nameof(name));
        NormalizedName = name.ToUpperInvariant();
        DisplayName = Guard.NotNullOrEmpty(displayName, nameof(displayName));
        IsActive = true;
    }

    /// <summary>
    /// Updates the tenant name.
    /// </summary>
    public void UpdateName(string name, string displayName)
    {
        Name = Guard.NotNullOrEmpty(name, nameof(name));
        NormalizedName = name.ToUpperInvariant();
        DisplayName = Guard.NotNullOrEmpty(displayName, nameof(displayName));
    }

    /// <summary>
    /// Activates the tenant.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Deactivates the tenant.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Sets the subscription period.
    /// </summary>
    public void SetSubscriptionPeriod(DateTime? startDate, DateTime? endDate)
    {
        if (startDate.HasValue && endDate.HasValue && endDate.Value < startDate.Value)
            throw new ArgumentException("End date must be after start date.");

        SubscriptionStartDate = startDate;
        SubscriptionEndDate = endDate;
    }

    /// <summary>
    /// Updates the tenant metadata.
    /// </summary>
    public void UpdateMetadata(string? metadata)
    {
        Metadata = metadata;
    }

    /// <summary>
    /// Checks if the tenant's subscription is active.
    /// </summary>
    public bool IsSubscriptionActive()
    {
        if (!IsActive)
            return false;

        var now = DateTime.UtcNow;

        if (SubscriptionStartDate.HasValue && now < SubscriptionStartDate.Value)
            return false;

        if (SubscriptionEndDate.HasValue && now > SubscriptionEndDate.Value)
            return false;

        return true;
    }
}
