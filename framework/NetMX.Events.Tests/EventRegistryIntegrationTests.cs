using Xunit;
using NetMX.Events;

namespace NetMX.Events.Tests;

/// <summary>
/// Integration tests for Event Registry system with real module events.
/// Tests cross-module event access and type safety through the Events static class.
/// </summary>
public class EventRegistryIntegrationTests
{
    [Fact]
    public void Events_Authorization_Permission_Events_Should_Exist()
    {
        // Verify Events.Permission.* is accessible from any module
        Assert.Equal("permission.created", Events.Permission.Created);
        Assert.Equal("permission.updated", Events.Permission.Updated);
        Assert.Equal("permission.deleted", Events.Permission.Deleted);
    }

    [Fact]
    public void Events_Authorization_Role_Events_Should_Exist()
    {
        // Verify Events.Role.* is accessible from any module
        Assert.Equal("role.created", Events.Role.Created);
        Assert.Equal("role.updated", Events.Role.Updated);
        Assert.Equal("role.deleted", Events.Role.Deleted);
    }

    [Fact]
    public void Events_Identity_User_Events_Should_Exist()
    {
        // Verify Events.User.* is accessible from any module
        Assert.Equal("user.registered", Events.User.Registered);
        Assert.Equal("user.profile.updated", Events.User.ProfileUpdated);
        Assert.Equal("user.deleted", Events.User.Deleted);
    }

    [Fact]
    public void Events_Identity_Login_Events_Should_Exist()
    {
        // Verify Events.User.Login.* is accessible from any module
        Assert.Equal("user.login.success", Events.User.Login.Success);
        Assert.Equal("user.login.failed", Events.User.Login.Failed);
        Assert.Equal("user.login.lockedout", Events.User.Login.LockedOut);
    }

    [Fact]
    public void Events_Audit_AuditLog_Events_Should_Exist()
    {
        // Verify Events.AuditLog.* is accessible from any module
        Assert.Equal("auditlog.created", Events.AuditLog.Created);
        Assert.Equal("auditlog.exported", Events.AuditLog.Exported);
        Assert.Equal("auditlog.deleted", Events.AuditLog.Deleted);
    }

    [Theory]
    [InlineData("permission.created")]
    [InlineData("role.created")]
    [InlineData("user.registered")]
    [InlineData("user.login.success")]
    [InlineData("auditlog.created")]
    public void Events_Should_Follow_Naming_Convention(string eventName)
    {
        // Verify lowercase format with dots (can have 2 or 3 segments)
        Assert.Matches(@"^[a-z]+(\.[a-z]+)+$", eventName);
    }

    [Fact]
    public void EventRegistry_Should_Register_And_Retrieve_Event()
    {
        // Arrange
        var registry = new EventRegistry();
        var metadata = new EventMetadata
        {
            Name = "test.created",
            Module = "Test",
            Category = "Test",
            Direction = EventDirection.Downstream,
            Description = "Test event"
        };

        // Act
        registry.RegisterEvent("test.created", metadata);
        var retrieved = registry.GetEvent("test.created");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("test.created", retrieved.Name);
        Assert.Equal("Test", retrieved.Module);
        Assert.Equal("Test", retrieved.Category);
    }

    [Fact]
    public void EventRegistry_Should_Prevent_Duplicate_Registration()
    {
        // Arrange
        var registry = new EventRegistry();
        var metadata = new EventMetadata
        {
            Name = "test.created",
            Module = "Test",
            Category = "Test",
            Direction = EventDirection.Downstream
        };
        registry.RegisterEvent("test.created", metadata);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => 
            registry.RegisterEvent("test.created", metadata));
        
        Assert.Contains("already registered", ex.Message);
    }

    [Fact]
    public void EventRegistry_Should_Return_All_Events()
    {
        // Arrange
        var registry = new EventRegistry();
        registry.RegisterEvent("test1.created", new EventMetadata
        {
            Name = "test1.created",
            Module = "Test",
            Category = "Test1"
        });
        registry.RegisterEvent("test2.created", new EventMetadata
        {
            Name = "test2.created",
            Module = "Test",
            Category = "Test2"
        });

        // Act
        var allEvents = registry.GetAllEvents();

        // Assert
        Assert.Equal(2, allEvents.Count());
    }

    [Fact]
    public void EventRegistry_Should_Find_Events_By_Name()
    {
        // Arrange
        var registry = new EventRegistry();
        registry.RegisterEvent("test.created", new EventMetadata
        {
            Name = "test.created",
            Module = "Test",
            Category = "Test"
        });

        // Act
        var found = registry.GetEvent("test.created");

        // Assert
        Assert.NotNull(found);
        Assert.Equal("test.created", found.Name);
        
        // Verify GetEvent throws for non-existent events
        Assert.Throws<KeyNotFoundException>(() => registry.GetEvent("nonexistent.event"));
    }

    [Fact]
    public void Events_Static_Class_Should_Be_Accessible_From_Any_Module()
    {
        // This test verifies the core benefit: no project references needed
        // Any module can access Events.* without referencing other modules

        // Authorization events accessible
        var permissionCreated = Events.Permission.Created;
        var roleCreated = Events.Role.Created;

        // Identity events accessible
        var userRegistered = Events.User.Registered;
        var loginSuccess = Events.User.Login.Success;

        // Audit events accessible
        var auditLogCreated = Events.AuditLog.Created;

        // All should be non-null and properly formatted
        Assert.NotNull(permissionCreated);
        Assert.NotNull(roleCreated);
        Assert.NotNull(userRegistered);
        Assert.NotNull(loginSuccess);
        Assert.NotNull(auditLogCreated);
        
        // Should not be empty strings
        Assert.NotEmpty(permissionCreated);
        Assert.NotEmpty(roleCreated);
        Assert.NotEmpty(userRegistered);
        Assert.NotEmpty(loginSuccess);
        Assert.NotEmpty(auditLogCreated);
    }

    [Fact]
    public void CrossModule_Event_Communication_Should_Be_Type_Safe()
    {
        // Scenario: Audit module wants to listen to Authorization events
        // This should work without Audit referencing Authorization

        // Audit can use Events.Permission.Created directly
        string eventToListenFor = Events.Permission.Created;

        // No need to know the string value, no magic strings
        Assert.Equal("permission.created", eventToListenFor);

        // IntelliSense shows this in real IDE
        // Compile-time safety: typos caught at compile time
    }

    [Fact]
    public void Event_Names_Should_Be_Unique_Constants()
    {
        // All event names should be unique string constants
        var permissionCreated = Events.Permission.Created;
        var roleCreated = Events.Role.Created;
        var userRegistered = Events.User.Registered;

        // No two events should have the same name
        Assert.NotEqual(permissionCreated, roleCreated);
        Assert.NotEqual(roleCreated, userRegistered);
        Assert.NotEqual(permissionCreated, userRegistered);
    }

    [Fact]
    public void EventMetadata_Should_Store_All_Required_Properties()
    {
        // Arrange & Act
        var metadata = new EventMetadata
        {
            Name = "test.created",
            Module = "Test",
            Category = "TestEntity",
            Direction = EventDirection.Downstream,
            Description = "Test entity was created"
        };

        // Assert
        Assert.Equal("test.created", metadata.Name);
        Assert.Equal("Test", metadata.Module);
        Assert.Equal("TestEntity", metadata.Category);
        Assert.Equal(EventDirection.Downstream, metadata.Direction);
        Assert.Equal("Test entity was created", metadata.Description);
    }

    [Fact]
    public void EventDirection_Should_Have_Both_Upstream_And_Downstream()
    {
        // Verify both directions exist for event flow control
        var upstream = EventDirection.Upstream;
        var downstream = EventDirection.Downstream;

        Assert.NotEqual(upstream, downstream);
    }
}
