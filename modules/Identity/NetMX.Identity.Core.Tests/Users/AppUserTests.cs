using NetMX.Identity.Core.Users;
using Xunit;

namespace NetMX.Identity.Core.Tests.Users;

public class AppUserTests
{
    [Fact]
    public void Constructor_CreatesUserWithCorrectProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userName = "testuser";
        var email = "test@example.com";
        var passwordHash = "hash123";
        var tenantId = Guid.NewGuid();

        // Act
        var user = new AppUser(id, userName, email, passwordHash, tenantId);

        // Assert
        Assert.Equal(id, user.Id);
        Assert.Equal(userName, user.UserName);
        Assert.Equal(email, user.Email);
        Assert.Equal(passwordHash, user.PasswordHash);
        Assert.Equal(tenantId, user.TenantId);
        Assert.True(user.IsActive);
        Assert.True(user.LockoutEnabled);
        Assert.False(user.EmailConfirmed);
        Assert.False(user.PhoneNumberConfirmed);
        Assert.False(user.TwoFactorEnabled);
        Assert.Equal(0, user.AccessFailedCount);
        Assert.NotNull(user.SecurityStamp);
    }

    [Fact]
    public void UpdateProfile_UpdatesUserProfile()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        user.UpdateProfile("John", "Doe", "123-456-7890");

        // Assert
        Assert.Equal("John", user.FirstName);
        Assert.Equal("Doe", user.LastName);
        Assert.Equal("123-456-7890", user.PhoneNumber);
    }

    [Fact]
    public void UpdateEmail_UpdatesEmailAndConfirmationStatus()
    {
        // Arrange
        var user = CreateTestUser();
        var newEmail = "newemail@example.com";

        // Act
        user.UpdateEmail(newEmail, true);

        // Assert
        Assert.Equal(newEmail, user.Email);
        Assert.True(user.EmailConfirmed);
    }

    [Fact]
    public void ConfirmEmail_SetsEmailConfirmedToTrue()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        user.ConfirmEmail();

        // Assert
        Assert.True(user.EmailConfirmed);
    }

    [Fact]
    public void UpdatePasswordHash_ChangesPasswordAndSecurityStamp()
    {
        // Arrange
        var user = CreateTestUser();
        var oldSecurityStamp = user.SecurityStamp;
        var newPasswordHash = "newhash456";

        // Act
        user.UpdatePasswordHash(newPasswordHash);

        // Assert
        Assert.Equal(newPasswordHash, user.PasswordHash);
        Assert.NotEqual(oldSecurityStamp, user.SecurityStamp);
    }

    [Fact]
    public void EnableTwoFactor_EnablesTwoFactorAuthentication()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        user.EnableTwoFactor();

        // Assert
        Assert.True(user.TwoFactorEnabled);
    }

    [Fact]
    public void DisableTwoFactor_DisablesTwoFactorAuthentication()
    {
        // Arrange
        var user = CreateTestUser();
        user.EnableTwoFactor();

        // Act
        user.DisableTwoFactor();

        // Assert
        Assert.False(user.TwoFactorEnabled);
    }

    [Fact]
    public void LockOut_LocksUserUntilSpecifiedDate()
    {
        // Arrange
        var user = CreateTestUser();
        var lockoutEnd = DateTime.UtcNow.AddMinutes(30);

        // Act
        user.LockOut(lockoutEnd);

        // Assert
        Assert.Equal(lockoutEnd, user.LockoutEnd);
        Assert.True(user.IsLockedOut());
    }

    [Fact]
    public void Unlock_UnlocksUserAndResetsFailedCount()
    {
        // Arrange
        var user = CreateTestUser();
        user.LockOut(DateTime.UtcNow.AddMinutes(30));
        user.RecordFailedLogin();

        // Act
        user.Unlock();

        // Assert
        Assert.Null(user.LockoutEnd);
        Assert.Equal(0, user.AccessFailedCount);
        Assert.False(user.IsLockedOut());
    }

    [Fact]
    public void RecordFailedLogin_IncrementsAccessFailedCount()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        user.RecordFailedLogin();
        user.RecordFailedLogin();

        // Assert
        Assert.Equal(2, user.AccessFailedCount);
    }

    [Fact]
    public void ResetAccessFailedCount_ResetsCountToZero()
    {
        // Arrange
        var user = CreateTestUser();
        user.RecordFailedLogin();
        user.RecordFailedLogin();

        // Act
        user.ResetAccessFailedCount();

        // Assert
        Assert.Equal(0, user.AccessFailedCount);
    }

    [Fact]
    public void Activate_ActivatesUser()
    {
        // Arrange
        var user = CreateTestUser();
        user.Deactivate();

        // Act
        user.Activate();

        // Assert
        Assert.True(user.IsActive);
    }

    [Fact]
    public void Deactivate_DeactivatesUser()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        user.Deactivate();

        // Assert
        Assert.False(user.IsActive);
    }

    [Fact]
    public void AddRole_AddsRoleToUser()
    {
        // Arrange
        var user = CreateTestUser();
        var roleId = Guid.NewGuid();

        // Act
        user.AddRole(roleId);

        // Assert
        Assert.Single(user.UserRoles);
        Assert.Equal(roleId, user.UserRoles.First().RoleId);
    }

    [Fact]
    public void AddRole_DoesNotAddDuplicateRole()
    {
        // Arrange
        var user = CreateTestUser();
        var roleId = Guid.NewGuid();

        // Act
        user.AddRole(roleId);
        user.AddRole(roleId);

        // Assert
        Assert.Single(user.UserRoles);
    }

    [Fact]
    public void RemoveRole_RemovesRoleFromUser()
    {
        // Arrange
        var user = CreateTestUser();
        var roleId = Guid.NewGuid();
        user.AddRole(roleId);

        // Act
        user.RemoveRole(roleId);

        // Assert
        Assert.Empty(user.UserRoles);
    }

    [Fact]
    public void AddClaim_AddsClaimToUser()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        user.AddClaim("email", "test@example.com");

        // Assert
        Assert.Single(user.Claims);
        Assert.Equal("email", user.Claims.First().ClaimType);
        Assert.Equal("test@example.com", user.Claims.First().ClaimValue);
    }

    [Fact]
    public void AddClaim_DoesNotAddDuplicateClaim()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        user.AddClaim("email", "test@example.com");
        user.AddClaim("email", "test@example.com");

        // Assert
        Assert.Single(user.Claims);
    }

    [Fact]
    public void RemoveClaim_RemovesClaimFromUser()
    {
        // Arrange
        var user = CreateTestUser();
        user.AddClaim("email", "test@example.com");

        // Act
        user.RemoveClaim("email", "test@example.com");

        // Assert
        Assert.Empty(user.Claims);
    }

    [Fact]
    public void GetFullName_ReturnsFullNameWhenBothNamesSet()
    {
        // Arrange
        var user = CreateTestUser();
        user.UpdateProfile("John", "Doe", null);

        // Act
        var fullName = user.GetFullName();

        // Assert
        Assert.Equal("John Doe", fullName);
    }

    [Fact]
    public void GetFullName_ReturnsFirstNameOnly()
    {
        // Arrange
        var user = CreateTestUser();
        user.UpdateProfile("John", null, null);

        // Act
        var fullName = user.GetFullName();

        // Assert
        Assert.Equal("John", fullName);
    }

    [Fact]
    public void GetFullName_ReturnsUserNameWhenNoNames()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var fullName = user.GetFullName();

        // Assert
        Assert.Equal("testuser", fullName);
    }

    private static AppUser CreateTestUser()
    {
        return new AppUser(
            Guid.NewGuid(),
            "testuser",
            "test@example.com",
            "hash123");
    }
}
