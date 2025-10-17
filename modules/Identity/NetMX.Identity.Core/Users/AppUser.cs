using NetMX.Ddd.Domain.Entities;

namespace NetMX.Identity.Core.Users;

/// <summary>
/// Represents a user in the system.
/// </summary>
public class AppUser : AggregateRoot<Guid>
{
    /// <summary>
    /// The unique username for login.
    /// </summary>
    public string UserName { get; private set; }

    /// <summary>
    /// The user's email address.
    /// </summary>
    public string Email { get; private set; }

    /// <summary>
    /// The user's email confirmation status.
    /// </summary>
    public bool EmailConfirmed { get; private set; }

    /// <summary>
    /// The hashed password.
    /// </summary>
    public string PasswordHash { get; private set; }

    /// <summary>
    /// Security stamp for invalidating tokens/sessions.
    /// </summary>
    public string SecurityStamp { get; private set; }

    /// <summary>
    /// The user's phone number.
    /// </summary>
    public string? PhoneNumber { get; private set; }

    /// <summary>
    /// The user's phone confirmation status.
    /// </summary>
    public bool PhoneNumberConfirmed { get; private set; }

    /// <summary>
    /// Whether two-factor authentication is enabled.
    /// </summary>
    public bool TwoFactorEnabled { get; private set; }

    /// <summary>
    /// The UTC date/time when lockout ends, if any.
    /// </summary>
    public DateTime? LockoutEnd { get; private set; }

    /// <summary>
    /// Whether lockout is enabled for this user.
    /// </summary>
    public bool LockoutEnabled { get; private set; }

    /// <summary>
    /// The number of failed login attempts.
    /// </summary>
    public int AccessFailedCount { get; private set; }

    /// <summary>
    /// The user's first name.
    /// </summary>
    public string? FirstName { get; private set; }

    /// <summary>
    /// The user's last name.
    /// </summary>
    public string? LastName { get; private set; }

    /// <summary>
    /// Whether the user is active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// The tenant ID for multi-tenant scenarios.
    /// </summary>
    public Guid? TenantId { get; private set; }

    // Navigation properties
    private readonly List<UserRole> _userRoles = new();
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    private readonly List<UserClaim> _claims = new();
    public IReadOnlyCollection<UserClaim> Claims => _claims.AsReadOnly();

    // EF Core constructor
    private AppUser() 
    {
        UserName = string.Empty;
        Email = string.Empty;
        PasswordHash = string.Empty;
        SecurityStamp = string.Empty;
    }

    /// <summary>
    /// Creates a new user.
    /// </summary>
    public AppUser(
        Guid id,
        string userName,
        string email,
        string passwordHash,
        Guid? tenantId = null)
    {
        Id = id;
        UserName = Guard.NotNullOrEmpty(userName, nameof(userName));
        Email = Guard.NotNullOrEmpty(email, nameof(email));
        PasswordHash = Guard.NotNullOrEmpty(passwordHash, nameof(passwordHash));
        SecurityStamp = Guid.NewGuid().ToString();
        TenantId = tenantId;
        IsActive = true;
        LockoutEnabled = true;
        EmailConfirmed = false;
        PhoneNumberConfirmed = false;
        TwoFactorEnabled = false;
        AccessFailedCount = 0;
    }

    /// <summary>
    /// Updates the user's profile information.
    /// </summary>
    public void UpdateProfile(string? firstName, string? lastName, string? phoneNumber)
    {
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
    }

    /// <summary>
    /// Updates the user's email.
    /// </summary>
    public void UpdateEmail(string email, bool confirmed = false)
    {
        Email = Guard.NotNullOrEmpty(email, nameof(email));
        EmailConfirmed = confirmed;
    }

    /// <summary>
    /// Confirms the user's email.
    /// </summary>
    public void ConfirmEmail()
    {
        EmailConfirmed = true;
    }

    /// <summary>
    /// Updates the user's phone number.
    /// </summary>
    public void UpdatePhoneNumber(string? phoneNumber, bool confirmed = false)
    {
        PhoneNumber = phoneNumber;
        PhoneNumberConfirmed = confirmed;
    }

    /// <summary>
    /// Confirms the user's phone number.
    /// </summary>
    public void ConfirmPhoneNumber()
    {
        PhoneNumberConfirmed = true;
    }

    /// <summary>
    /// Updates the user's password hash.
    /// </summary>
    public void UpdatePasswordHash(string passwordHash)
    {
        PasswordHash = Guard.NotNullOrEmpty(passwordHash, nameof(passwordHash));
        SecurityStamp = Guid.NewGuid().ToString(); // Invalidate existing sessions
    }

    /// <summary>
    /// Enables two-factor authentication.
    /// </summary>
    public void EnableTwoFactor()
    {
        TwoFactorEnabled = true;
    }

    /// <summary>
    /// Disables two-factor authentication.
    /// </summary>
    public void DisableTwoFactor()
    {
        TwoFactorEnabled = false;
    }

    /// <summary>
    /// Locks out the user until the specified date/time.
    /// </summary>
    public void LockOut(DateTime lockoutEnd)
    {
        if (!LockoutEnabled)
            throw new InvalidOperationException("Lockout is not enabled for this user.");

        LockoutEnd = lockoutEnd;
    }

    /// <summary>
    /// Unlocks the user.
    /// </summary>
    public void Unlock()
    {
        LockoutEnd = null;
        AccessFailedCount = 0;
    }

    /// <summary>
    /// Records a failed login attempt.
    /// </summary>
    public void RecordFailedLogin()
    {
        AccessFailedCount++;
    }

    /// <summary>
    /// Resets the failed login count.
    /// </summary>
    public void ResetAccessFailedCount()
    {
        AccessFailedCount = 0;
    }

    /// <summary>
    /// Activates the user.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Deactivates the user.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Adds a role to the user.
    /// </summary>
    public void AddRole(Guid roleId)
    {
        if (_userRoles.Any(ur => ur.RoleId == roleId))
            return;

        _userRoles.Add(new UserRole(Id, roleId));
    }

    /// <summary>
    /// Removes a role from the user.
    /// </summary>
    public void RemoveRole(Guid roleId)
    {
        var userRole = _userRoles.FirstOrDefault(ur => ur.RoleId == roleId);
        if (userRole != null)
            _userRoles.Remove(userRole);
    }

    /// <summary>
    /// Adds a claim to the user.
    /// </summary>
    public void AddClaim(string claimType, string claimValue)
    {
        if (_claims.Any(c => c.ClaimType == claimType && c.ClaimValue == claimValue))
            return;

        _claims.Add(new UserClaim(Id, claimType, claimValue));
    }

    /// <summary>
    /// Removes a claim from the user.
    /// </summary>
    public void RemoveClaim(string claimType, string claimValue)
    {
        var claim = _claims.FirstOrDefault(c => c.ClaimType == claimType && c.ClaimValue == claimValue);
        if (claim != null)
            _claims.Remove(claim);
    }

    /// <summary>
    /// Checks if the user is currently locked out.
    /// </summary>
    public bool IsLockedOut()
    {
        return LockoutEnd.HasValue && LockoutEnd.Value > DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the user's full name.
    /// </summary>
    public string GetFullName()
    {
        if (!string.IsNullOrEmpty(FirstName) && !string.IsNullOrEmpty(LastName))
            return $"{FirstName} {LastName}";
        
        if (!string.IsNullOrEmpty(FirstName))
            return FirstName;
        
        if (!string.IsNullOrEmpty(LastName))
            return LastName;
        
        return UserName;
    }
}
