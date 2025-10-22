using FluentAssertions;
using NetMX.Events;
using Xunit;

namespace NetMX.Events.Tests;

/// <summary>
/// Tests for type-safe static event constants (Events.Permission.Created, etc.)
/// </summary>
[Collection("EventsCollection")] // Prevent parallel execution
public class TypeSafeEventsTests
{
    public TypeSafeEventsTests()
    {
        Events.Reset();
    }
    
    [Fact]
    public void AuthorizationEvents_PermissionConstants_AreAccessible()
    {
        // Assert - Type-safe constants exist and have correct values
        Events.Permission.Created.Should().Be("permission.created");
        Events.Permission.Updated.Should().Be("permission.updated");
        Events.Permission.Deleted.Should().Be("permission.deleted");
    }
    
    [Fact]
    public void AuthorizationEvents_RoleConstants_AreAccessible()
    {
        // Assert
        Events.Role.Created.Should().Be("role.created");
        Events.Role.Updated.Should().Be("role.updated");
        Events.Role.Deleted.Should().Be("role.deleted");
        Events.Role.PermissionGranted.Should().Be("role.permission.granted");
        Events.Role.PermissionRevoked.Should().Be("role.permission.revoked");
    }
    
    [Fact]
    public void IdentityEvents_UserConstants_AreAccessible()
    {
        // Assert
        Events.User.Registered.Should().Be("user.registered");
        Events.User.ProfileUpdated.Should().Be("user.profile.updated");
        Events.User.EmailConfirmed.Should().Be("user.email.confirmed");
        Events.User.Deleted.Should().Be("user.deleted");
    }
    
    [Fact]
    public void IdentityEvents_LoginConstants_AreAccessible()
    {
        // Assert - Nested class structure
        Events.User.Login.Success.Should().Be("user.login.success");
        Events.User.Login.Failed.Should().Be("user.login.failed");
        Events.User.Login.LockedOut.Should().Be("user.login.lockedout");
        Events.User.Login.TwoFactorRequired.Should().Be("user.login.twofactor.required");
    }
    
    [Fact]
    public void IdentityEvents_SessionConstants_AreAccessible()
    {
        // Assert
        Events.User.Session.Created.Should().Be("user.session.created");
        Events.User.Session.Renewed.Should().Be("user.session.renewed");
        Events.User.Session.Expired.Should().Be("user.session.expired");
    }
    
    [Fact]
    public void IdentityEvents_PasswordConstants_AreAccessible()
    {
        // Assert
        Events.User.Password.Changed.Should().Be("user.password.changed");
        Events.User.Password.ResetRequested.Should().Be("user.password.reset.requested");
        Events.User.Password.ResetCompleted.Should().Be("user.password.reset.completed");
    }
    
    [Fact]
    public void IdentityEvents_AccountConstants_AreAccessible()
    {
        // Assert
        Events.User.Account.Locked.Should().Be("user.account.locked");
        Events.User.Account.Unlocked.Should().Be("user.account.unlocked");
    }
    
    [Fact]
    public void AuditEvents_AuditLogConstants_AreAccessible()
    {
        // Assert
        Events.AuditLog.Created.Should().Be("auditlog.created");
        Events.AuditLog.Viewed.Should().Be("auditlog.viewed");
        Events.AuditLog.Exported.Should().Be("auditlog.exported");
    }
    
    [Fact]
    public void AuditEvents_AuditEntryConstants_AreAccessible()
    {
        // Assert
        Events.AuditEntry.Recorded.Should().Be("auditentry.recorded");
        Events.AuditEntry.Updated.Should().Be("auditentry.updated");
    }
    
    [Fact]
    public void AuditEvents_EntityChangeConstants_AreAccessible()
    {
        // Assert
        Events.EntityChange.Tracked.Should().Be("entitychange.tracked");
        Events.EntityChange.PropertyChanged.Should().Be("entitychange.property.changed");
    }
    
    [Fact]
    public void AuditEvents_ComplianceConstants_AreAccessible()
    {
        // Assert
        Events.Compliance.ReportGenerated.Should().Be("compliance.report.generated");
        Events.Compliance.ViolationDetected.Should().Be("compliance.violation.detected");
        Events.Compliance.PolicyUpdated.Should().Be("compliance.policy.updated");
    }
    
    [Fact]
    public void TypeSafeConstants_CanBeUsedWithRegistry()
    {
        // Arrange
        var registry = new EventRegistry();
        
        // Register using type-safe constants
        registry.RegisterEvent(Events.Permission.Created, new EventMetadata
        {
            Name = Events.Permission.Created,
            Module = "Authorization",
            Category = "Permission"
        });
        
        registry.RegisterEvent(Events.User.Login.Success, new EventMetadata
        {
            Name = Events.User.Login.Success,
            Module = "Identity",
            Category = "Login"
        });
        
        Events.Initialize(registry);
        
        // Act & Assert - Can retrieve using same constants
        Events.Exists(Events.Permission.Created).Should().BeTrue();
        Events.Exists(Events.User.Login.Success).Should().BeTrue();
        
        Events.Get(Events.Permission.Created).Should().Be("permission.created");
        Events.Get(Events.User.Login.Success).Should().Be("user.login.success");
    }
    
    [Fact]
    public void TypeSafeConstants_WorkAcrossModuleBoundaries()
    {
        // Arrange
        var registry = new EventRegistry();
        
        // Authorization module registers its events
        registry.RegisterEvent(Events.Permission.Created, new EventMetadata
        {
            Name = Events.Permission.Created,
            Module = "Authorization",
            Category = "Permission"
        });
        
        // Identity module registers its events
        registry.RegisterEvent(Events.User.Registered, new EventMetadata
        {
            Name = Events.User.Registered,
            Module = "Identity",
            Category = "User"
        });
        
        // Audit module registers its events
        registry.RegisterEvent(Events.AuditLog.Created, new EventMetadata
        {
            Name = Events.AuditLog.Created,
            Module = "Audit",
            Category = "AuditLog"
        });
        
        Events.Initialize(registry);
        
        // Act & Assert - All modules can access all events without project references!
        // Audit module can listen to Authorization events:
        Events.Exists(Events.Permission.Created).Should().BeTrue();
        
        // Authorization module can listen to Identity events:
        Events.Exists(Events.User.Registered).Should().BeTrue();
        
        // Identity module can listen to Audit events:
        Events.Exists(Events.AuditLog.Created).Should().BeTrue();
        
        // All without any project references between modules!
    }
}
