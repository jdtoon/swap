using Microsoft.AspNetCore.Identity;
using NetMX.Ddd.Domain;

namespace NetMX.Identity.Core.Users;

/// <summary>
/// Represents a role in the system.
/// Extends ASP.NET Core Identity with custom properties and business logic.
/// </summary>
public class AppRole : IdentityRole<Guid>, IMultiTenant, ISoftDelete, IHasConcurrencyStamp
{
    // Note: Name, NormalizedName, ConcurrencyStamp are inherited from IdentityRole<Guid>

    /// <summary>
    /// The role description.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Whether this is a system role (cannot be deleted).
    /// </summary>
    public bool IsSystemRole { get; private set; }

    /// <summary>
    /// The tenant ID for multi-tenant scenarios.
    /// </summary>
    public Guid? TenantId { get; private set; }

    // Navigation properties
    private readonly List<RoleClaim> _claims = new();
    public IReadOnlyCollection<RoleClaim> Claims => _claims.AsReadOnly();

    // EF Core constructor
    private AppRole()
    {
    }

    /// <summary>
    /// Creates a new role.
    /// </summary>
    public AppRole(
        Guid id,
        string name,
        string? description = null,
        bool isSystemRole = false,
        Guid? tenantId = null)
    {
        Id = id;
        Name = Guard.NotNullOrEmpty(name, nameof(name));
        NormalizedName = name.ToUpperInvariant();
        ConcurrencyStamp = Guid.NewGuid().ToString();
        Description = description;
        IsSystemRole = isSystemRole;
        TenantId = tenantId;
    }

    /// <summary>
    /// Updates the role name.
    /// </summary>
    public void UpdateName(string name)
    {
        Name = Guard.NotNullOrEmpty(name, nameof(name));
        NormalizedName = name.ToUpperInvariant();
    }

    /// <summary>
    /// Updates the role description.
    /// </summary>
    public void UpdateDescription(string? description)
    {
        Description = description;
    }

    /// <summary>
    /// Adds a claim to the role.
    /// </summary>
    public void AddClaim(string claimType, string claimValue)
    {
        if (_claims.Any(c => c.ClaimType == claimType && c.ClaimValue == claimValue))
            return;

        _claims.Add(new RoleClaim(Id, claimType, claimValue));
    }

    /// <summary>
    /// Removes a claim from the role.
    /// </summary>
    public void RemoveClaim(string claimType, string claimValue)
    {
        var claim = _claims.FirstOrDefault(c => c.ClaimType == claimType && c.ClaimValue == claimValue);
        if (claim != null)
            _claims.Remove(claim);
    }
}
