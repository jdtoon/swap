using FluentAssertions;
using NetMX.Events;
using Xunit;

namespace NetMX.Events.Tests;

/// <summary>
/// Tests for <see cref="Events"/> static class.
/// </summary>
[Collection("EventsCollection")] // Prevent parallel execution
public class EventsStaticClassTests
{
    public EventsStaticClassTests()
    {
        // Reset Events before each test
        Events.Reset();
    }
    
    [Fact]
    public void Initialize_WithValidRegistry_Succeeds()
    {
        // Arrange
        var registry = new EventRegistry();
        registry.RegisterEvent("permission.created", new EventMetadata
        {
            Name = "permission.created",
            Module = "Authorization",
            Category = "Permission"
        });
        
        // Act
        var act = () => Events.Initialize(registry);
        
        // Assert
        act.Should().NotThrow();
    }
    
    [Fact]
    public void Initialize_NullRegistry_ThrowsArgumentNullException()
    {
        // Act
        var act = () => Events.Initialize(null!);
        
        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("registry");
    }
    
    [Fact]
    public void Initialize_CalledTwice_ThrowsInvalidOperationException()
    {
        // Arrange
        var registry = new EventRegistry();
        Events.Initialize(registry);
        
        // Act
        var act = () => Events.Initialize(registry);
        
        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already been initialized*");
    }
    
    [Fact]
    public void Get_RegisteredEvent_ReturnsEventName()
    {
        // Arrange
        var registry = new EventRegistry();
        registry.RegisterEvent("permission.created", new EventMetadata
        {
            Name = "permission.created",
            Module = "Authorization",
            Category = "Permission"
        });
        
        Events.Initialize(registry);
        
        // Act
        var result = Events.Get("permission.created");
        
        // Assert
        result.Should().Be("permission.created");
    }
    
    [Fact]
    public void Get_UnregisteredEvent_ThrowsKeyNotFoundException()
    {
        // Arrange
        var registry = new EventRegistry();
        Events.Initialize(registry);
        
        // Act
        var act = () => Events.Get("nonexistent.event");
        
        // Assert
        act.Should().Throw<KeyNotFoundException>()
            .WithMessage("*not registered*");
    }
    
    [Fact]
    public void Get_NotInitialized_ThrowsInvalidOperationException()
    {
        // Act
        var act = () => Events.Get("permission.created");
        
        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not been initialized*");
    }
    
    [Fact]
    public void Exists_RegisteredEvent_ReturnsTrue()
    {
        // Arrange
        var registry = new EventRegistry();
        registry.RegisterEvent("permission.created", new EventMetadata
        {
            Name = "permission.created",
            Module = "Authorization",
            Category = "Permission"
        });
        
        Events.Initialize(registry);
        
        // Act
        var result = Events.Exists("permission.created");
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Fact]
    public void Exists_UnregisteredEvent_ReturnsFalse()
    {
        // Arrange
        var registry = new EventRegistry();
        Events.Initialize(registry);
        
        // Act
        var result = Events.Exists("nonexistent.event");
        
        // Assert
        result.Should().BeFalse();
    }
    
    [Fact]
    public void Exists_NotInitialized_ThrowsInvalidOperationException()
    {
        // Act
        var act = () => Events.Exists("permission.created");
        
        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not been initialized*");
    }
    
    [Fact]
    public void GetAll_AfterRegistration_ReturnsAllEvents()
    {
        // Arrange
        var registry = new EventRegistry();
        registry.RegisterEvent("permission.created", new EventMetadata
        {
            Name = "permission.created",
            Module = "Authorization",
            Category = "Permission"
        });
        
        registry.RegisterEvent("role.created", new EventMetadata
        {
            Name = "role.created",
            Module = "Authorization",
            Category = "Role"
        });
        
        Events.Initialize(registry);
        
        // Act
        var result = Events.GetAll();
        
        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainKey("permission.created");
        result.Should().ContainKey("role.created");
    }
    
    [Fact]
    public void GetAll_NotInitialized_ThrowsInvalidOperationException()
    {
        // Act
        var act = () => Events.GetAll();
        
        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not been initialized*");
    }
    
    [Fact]
    public void GetMetadata_RegisteredEvent_ReturnsMetadata()
    {
        // Arrange
        var registry = new EventRegistry();
        registry.RegisterEvent("permission.created", new EventMetadata
        {
            Name = "permission.created",
            Module = "Authorization",
            Category = "Permission",
            Description = "Test event"
        });
        
        Events.Initialize(registry);
        
        // Act
        var result = Events.GetMetadata("permission.created");
        
        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("permission.created");
        result.Module.Should().Be("Authorization");
        result.Category.Should().Be("Permission");
        result.Description.Should().Be("Test event");
    }
    
    [Fact]
    public void GetMetadata_UnregisteredEvent_ThrowsKeyNotFoundException()
    {
        // Arrange
        var registry = new EventRegistry();
        Events.Initialize(registry);
        
        // Act
        var act = () => Events.GetMetadata("nonexistent.event");
        
        // Assert
        act.Should().Throw<KeyNotFoundException>()
            .WithMessage("*not registered*");
    }
    
    [Fact]
    public void GetMetadata_NotInitialized_ThrowsInvalidOperationException()
    {
        // Act
        var act = () => Events.GetMetadata("permission.created");
        
        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not been initialized*");
    }
    
    [Fact]
    public void Reset_AfterInitialization_AllowsReinitialization()
    {
        // Arrange
        var registry1 = new EventRegistry();
        registry1.RegisterEvent("permission.created", new EventMetadata
        {
            Name = "permission.created",
            Module = "Authorization",
            Category = "Permission"
        });
        
        Events.Initialize(registry1);
        
        // Act
        Events.Reset();
        
        var registry2 = new EventRegistry();
        registry2.RegisterEvent("role.created", new EventMetadata
        {
            Name = "role.created",
            Module = "Authorization",
            Category = "Role"
        });
        
        Events.Initialize(registry2);
        
        // Assert
        Events.Exists("role.created").Should().BeTrue();
        Events.Exists("permission.created").Should().BeFalse();
    }
}
