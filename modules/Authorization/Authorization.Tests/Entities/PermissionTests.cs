using Authorization.Core.Entities;

namespace Authorization.Tests.Entities;

/// <summary>
/// Unit tests for Permission entity.
/// Tests entity creation, validation, and domain logic.
/// </summary>
public class PermissionTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesPermission()
    {
        // Arrange
        var id = Guid.NewGuid();
        var name = "Users.View";
        var displayName = "View Users";
        var group = "Users";

        // Act
        var permission = new Permission(id, name, displayName, group);

        // Assert
        Assert.Equal(id, permission.Id);
        Assert.Equal(name, permission.Name);
        Assert.Equal(displayName, permission.DisplayName);
        Assert.Equal(group, permission.Group);
        Assert.True(permission.IsActive);
        Assert.False(permission.IsSystemPermission);
        Assert.Null(permission.Description);
    }

    [Fact]
    public void Constructor_WithDescription_CreatesPermissionWithDescription()
    {
        // Arrange
        var id = Guid.NewGuid();
        var description = "Allows viewing user list and details";

        // Act
        var permission = new Permission(
            id, 
            "Users.View", 
            "View Users", 
            "Users", 
            description);

        // Assert
        Assert.Equal(description, permission.Description);
    }

    [Fact]
    public void Constructor_WithSystemPermissionFlag_CreatesSystemPermission()
    {
        // Arrange & Act
        var permission = new Permission(
            Guid.NewGuid(),
            "System.Admin",
            "System Administrator",
            "System",
            isSystemPermission: true);

        // Assert
        Assert.True(permission.IsSystemPermission);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_ThrowsArgumentException(string? invalidName)
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new Permission(Guid.NewGuid(), invalidName!, "Display", "Group"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidDisplayName_ThrowsArgumentException(string? invalidDisplayName)
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new Permission(Guid.NewGuid(), "Name", invalidDisplayName!, "Group"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidGroup_ThrowsArgumentException(string? invalidGroup)
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new Permission(Guid.NewGuid(), "Name", "Display", invalidGroup!));
    }

    [Fact]
    public void UpdateDetails_WithValidParameters_UpdatesPermission()
    {
        // Arrange
        var permission = new Permission(
            Guid.NewGuid(),
            "Users.View",
            "View Users",
            "Users");

        var newDisplayName = "View All Users";
        var newGroup = "User Management";
        var newDescription = "Updated description";

        // Act
        permission.UpdateDetails(newDisplayName, newGroup, newDescription);

        // Assert
        Assert.Equal(newDisplayName, permission.DisplayName);
        Assert.Equal(newGroup, permission.Group);
        Assert.Equal(newDescription, permission.Description);
        Assert.Equal("Users.View", permission.Name); // Name should not change
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateDetails_WithInvalidDisplayName_ThrowsArgumentException(string? invalidDisplayName)
    {
        // Arrange
        var permission = new Permission(Guid.NewGuid(), "Name", "Display", "Group");

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            permission.UpdateDetails(invalidDisplayName!, "Group", null));
    }

    [Fact]
    public void Deactivate_WhenActive_DeactivatesPermission()
    {
        // Arrange
        var permission = new Permission(Guid.NewGuid(), "Users.View", "View Users", "Users");
        Assert.True(permission.IsActive); // Precondition

        // Act
        permission.Deactivate();

        // Assert
        Assert.False(permission.IsActive);
    }

    [Fact]
    public void Activate_WhenInactive_ActivatesPermission()
    {
        // Arrange
        var permission = new Permission(Guid.NewGuid(), "Users.View", "View Users", "Users");
        permission.Deactivate();
        Assert.False(permission.IsActive); // Precondition

        // Act
        permission.Activate();

        // Assert
        Assert.True(permission.IsActive);
    }

    [Fact]
    public void Deactivate_SystemPermission_ThrowsInvalidOperationException()
    {
        // Arrange
        var permission = new Permission(
            Guid.NewGuid(),
            "System.Admin",
            "System Administrator",
            "System",
            isSystemPermission: true);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => permission.Deactivate());
        Assert.Contains("system permission", exception.Message.ToLower());
    }

    [Fact]
    public void Permission_NameFormat_FollowsConvention()
    {
        // Arrange & Act
        var permission = new Permission(
            Guid.NewGuid(),
            "Users.View",
            "View Users",
            "Users");

        // Assert - Name should follow Group.Action convention
        Assert.Contains(".", permission.Name);
        var parts = permission.Name.Split('.');
        Assert.Equal(2, parts.Length);
        Assert.Equal("Users", parts[0]);
        Assert.Equal("View", parts[1]);
    }
}
