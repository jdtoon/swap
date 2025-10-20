using Authorization.Core.Entities;

namespace Authorization.Tests.Entities;

/// <summary>
/// Unit tests for Role entity.
/// Tests role creation, permission management, and domain logic.
/// </summary>
public class RoleTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesRole()
    {
        // Arrange
        var id = Guid.NewGuid();
        var name = "Admin";
        var description = "Administrator role";

        // Act
        var role = new Role(id, name, description);

        // Assert
        Assert.Equal(id, role.Id);
        Assert.Equal(name, role.Name);
        Assert.Equal(description, role.Description);
        Assert.False(role.IsSystemRole);
        Assert.False(role.IsDefault);
        Assert.Empty(role.RolePermissions);
    }

    [Fact]
    public void Constructor_WithSystemRoleFlag_CreatesSystemRole()
    {
        // Arrange & Act
        var role = new Role(
            Guid.NewGuid(),
            "Admin",
            "Administrator",
            isSystemRole: true);

        // Assert
        Assert.True(role.IsSystemRole);
    }

    [Fact]
    public void Constructor_WithDefaultFlag_CreatesDefaultRole()
    {
        // Arrange & Act
        var role = new Role(
            Guid.NewGuid(),
            "User",
            "Standard user",
            isDefault: true);

        // Assert
        Assert.True(role.IsDefault);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_ThrowsArgumentException(string? invalidName)
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new Role(Guid.NewGuid(), invalidName!, "Description"));
    }

    [Fact]
    public void UpdateDetails_WithValidParameters_UpdatesRole()
    {
        // Arrange
        var role = new Role(Guid.NewGuid(), "Admin", "Administrator");
        var newName = "SuperAdmin";
        var newDescription = "Super Administrator";

        // Act
        role.UpdateDetails(newName, newDescription);

        // Assert
        Assert.Equal(newName, role.Name);
        Assert.Equal(newDescription, role.Description);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateDetails_WithInvalidName_ThrowsArgumentException(string? invalidName)
    {
        // Arrange
        var role = new Role(Guid.NewGuid(), "Admin", "Administrator");

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            role.UpdateDetails(invalidName!, "Description"));
    }

    [Fact]
    public void GrantPermission_WithValidPermission_AddsPermission()
    {
        // Arrange
        var role = new Role(Guid.NewGuid(), "Admin", "Administrator");
        var permissionId = Guid.NewGuid();
        var grantedBy = Guid.NewGuid();
        var grantedAt = DateTime.UtcNow;

        // Act
        role.GrantPermission(permissionId, grantedBy, grantedAt);

        // Assert
        Assert.Single(role.RolePermissions);
        var rolePermission = role.RolePermissions.First();
        Assert.Equal(role.Id, rolePermission.RoleId);
        Assert.Equal(permissionId, rolePermission.PermissionId);
        Assert.Equal(grantedBy, rolePermission.GrantedBy);
        Assert.Equal(grantedAt, rolePermission.GrantedAt);
    }

    [Fact]
    public void GrantPermission_WhenAlreadyGranted_DoesNotDuplicate()
    {
        // Arrange
        var role = new Role(Guid.NewGuid(), "Admin", "Administrator");
        var permissionId = Guid.NewGuid();
        var grantedBy = Guid.NewGuid();

        // Act - Grant same permission twice
        role.GrantPermission(permissionId, grantedBy, DateTime.UtcNow);
        role.GrantPermission(permissionId, grantedBy, DateTime.UtcNow);

        // Assert - Should only have one
        Assert.Single(role.RolePermissions);
    }

    [Fact]
    public void RevokePermission_WhenPermissionExists_RemovesPermission()
    {
        // Arrange
        var role = new Role(Guid.NewGuid(), "Admin", "Administrator");
        var permissionId = Guid.NewGuid();
        role.GrantPermission(permissionId, Guid.NewGuid(), DateTime.UtcNow);
        Assert.Single(role.RolePermissions); // Precondition

        // Act
        role.RevokePermission(permissionId);

        // Assert
        Assert.Empty(role.RolePermissions);
    }

    [Fact]
    public void RevokePermission_WhenPermissionDoesNotExist_DoesNothing()
    {
        // Arrange
        var role = new Role(Guid.NewGuid(), "Admin", "Administrator");
        var permissionId = Guid.NewGuid();

        // Act - Should not throw
        role.RevokePermission(permissionId);

        // Assert
        Assert.Empty(role.RolePermissions);
    }

    [Fact]
    public void RevokeAllPermissions_RemovesAllPermissions()
    {
        // Arrange
        var role = new Role(Guid.NewGuid(), "Admin", "Administrator");
        var grantedBy = Guid.NewGuid();
        var now = DateTime.UtcNow;
        
        role.GrantPermission(Guid.NewGuid(), grantedBy, now);
        role.GrantPermission(Guid.NewGuid(), grantedBy, now);
        role.GrantPermission(Guid.NewGuid(), grantedBy, now);
        
        Assert.Equal(3, role.RolePermissions.Count); // Precondition

        // Act
        role.RevokeAllPermissions();

        // Assert
        Assert.Empty(role.RolePermissions);
    }

    [Fact]
    public void HasPermission_WhenPermissionGranted_ReturnsTrue()
    {
        // Arrange
        var role = new Role(Guid.NewGuid(), "Admin", "Administrator");
        var permissionId = Guid.NewGuid();
        role.GrantPermission(permissionId, Guid.NewGuid(), DateTime.UtcNow);

        // Act
        var hasPermission = role.HasPermission(permissionId);

        // Assert
        Assert.True(hasPermission);
    }

    [Fact]
    public void HasPermission_WhenPermissionNotGranted_ReturnsFalse()
    {
        // Arrange
        var role = new Role(Guid.NewGuid(), "Admin", "Administrator");
        var permissionId = Guid.NewGuid();

        // Act
        var hasPermission = role.HasPermission(permissionId);

        // Assert
        Assert.False(hasPermission);
    }

    [Fact]
    public void SetAsDefault_SetsDefaultFlag()
    {
        // Arrange
        var role = new Role(Guid.NewGuid(), "User", "Standard user", isDefault: false);
        Assert.False(role.IsDefault); // Precondition

        // Act
        role.SetAsDefault();

        // Assert
        Assert.True(role.IsDefault);
    }

    [Fact]
    public void UnsetAsDefault_RemovesDefaultFlag()
    {
        // Arrange
        var role = new Role(Guid.NewGuid(), "User", "Standard user", isDefault: true);
        Assert.True(role.IsDefault); // Precondition

        // Act
        role.UnsetAsDefault();

        // Assert
        Assert.False(role.IsDefault);
    }

    [Fact]
    public void UpdateDetails_SystemRole_ThrowsInvalidOperationException()
    {
        // Arrange
        var role = new Role(
            Guid.NewGuid(),
            "Admin",
            "Administrator",
            isSystemRole: true);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            role.UpdateDetails("NewName", "New Description"));
        Assert.Contains("system role", exception.Message.ToLower());
    }
}
