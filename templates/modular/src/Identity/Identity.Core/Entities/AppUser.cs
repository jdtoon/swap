using NetMX.Ddd.Domain.Entities;

namespace Identity.Core.Entities;

/// <summary>
/// Represents a user in the application.
/// This is a simplified identity system - for production, consider integrating with
/// ASP.NET Core Identity or an external provider like Logto (which we'll do in Task 4.6).
/// </summary>
public class AppUser : AggregateRoot<Guid>
{
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginDate { get; set; }

    // Private constructor for EF Core
    private AppUser() { }

    // Factory method (enforces business rules)
    public static AppUser Create(
        string email,
        string passwordHash,
        string? fullName = null,
        string? phoneNumber = null,
        bool emailConfirmed = false,
        bool phoneNumberConfirmed = false)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));

        if (!IsValidEmail(email))
            throw new ArgumentException("Invalid email format", nameof(email));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required", nameof(passwordHash));

        return new AppUser
        {
            Id = Guid.NewGuid(),
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            FullName = fullName,
            PhoneNumber = phoneNumber,
            EmailConfirmed = emailConfirmed,
            PhoneNumberConfirmed = phoneNumberConfirmed,
            IsActive = true
        };
    }

    // Business logic methods
    public void UpdateProfile(string? fullName, string? phoneNumber)
    {
        FullName = fullName;
        PhoneNumber = phoneNumber;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void RecordLogin()
    {
        LastLoginDate = DateTime.UtcNow;
    }

    public void ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("Password hash is required", nameof(newPasswordHash));

        PasswordHash = newPasswordHash;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
