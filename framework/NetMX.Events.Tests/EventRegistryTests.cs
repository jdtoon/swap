using FluentAssertions;
using NetMX.Events;
using Xunit;

namespace NetMX.Events.Tests;

/// <summary>
/// Tests for <see cref="EventRegistry"/>.
/// </summary>
public class EventRegistryTests
{
    private readonly EventRegistry _registry;
    
    public EventRegistryTests()
    {
        _registry = new EventRegistry();
    }
    
    [Fact]
    public void RegisterEvent_UniqueEvent_Succeeds()
    {
        // Arrange
        var metadata = new EventMetadata
        {
            Name = "permission.created",
            Module = "Authorization",
            Category = "Permission"
        };
        
        // Act
        var act = () => _registry.RegisterEvent("permission.created", metadata);
        
        // Assert
        act.Should().NotThrow();
        _registry.IsRegistered("permission.created").Should().BeTrue();
    }
    
    [Fact]
    public void RegisterEvent_DuplicateEvent_ThrowsInvalidOperationException()
    {
        // Arrange
        var metadata1 = new EventMetadata
        {
            Name = "permission.created",
            Module = "Authorization",
            Category = "Permission"
        };
        
        var metadata2 = new EventMetadata
        {
            Name = "permission.created",
            Module = "AnotherModule",
            Category = "Permission"
        };
        
        _registry.RegisterEvent("permission.created", metadata1);
        
        // Act
        var act = () => _registry.RegisterEvent("permission.created", metadata2);
        
        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already registered*Authorization*")
            .WithMessage("*duplicate*AnotherModule*");
    }
    
    [Fact]
    public void RegisterEvent_NullName_ThrowsArgumentException()
    {
        // Arrange
        var metadata = new EventMetadata
        {
            Name = "permission.created",
            Module = "Authorization",
            Category = "Permission"
        };
        
        // Act
        var act = () => _registry.RegisterEvent(null!, metadata);
        
        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }
    
    [Fact]
    public void RegisterEvent_EmptyName_ThrowsArgumentException()
    {
        // Arrange
        var metadata = new EventMetadata
        {
            Name = "permission.created",
            Module = "Authorization",
            Category = "Permission"
        };
        
        // Act
        var act = () => _registry.RegisterEvent("", metadata);
        
        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }
    
    [Fact]
    public void RegisterEvent_NullMetadata_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _registry.RegisterEvent("permission.created", null!);
        
        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("metadata");
    }
    
    [Fact]
    public void RegisterEvent_NameMismatch_ThrowsArgumentException()
    {
        // Arrange
        var metadata = new EventMetadata
        {
            Name = "permission.created",
            Module = "Authorization",
            Category = "Permission"
        };
        
        // Act
        var act = () => _registry.RegisterEvent("permission.updated", metadata);
        
        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*does not match metadata name*");
    }
    
    [Fact]
    public void GetEvent_RegisteredEvent_ReturnsMetadata()
    {
        // Arrange
        var metadata = new EventMetadata
        {
            Name = "permission.created",
            Module = "Authorization",
            Category = "Permission",
            Description = "Test event"
        };
        
        _registry.RegisterEvent("permission.created", metadata);
        
        // Act
        var result = _registry.GetEvent("permission.created");
        
        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("permission.created");
        result.Module.Should().Be("Authorization");
        result.Category.Should().Be("Permission");
        result.Description.Should().Be("Test event");
    }
    
    [Fact]
    public void GetEvent_UnregisteredEvent_ThrowsKeyNotFoundException()
    {
        // Act
        var act = () => _registry.GetEvent("nonexistent.event");
        
        // Assert
        act.Should().Throw<KeyNotFoundException>()
            .WithMessage("*not registered*");
    }
    
    [Fact]
    public void GetEvent_NullName_ThrowsArgumentException()
    {
        // Act
        var act = () => _registry.GetEvent(null!);
        
        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }
    
    [Fact]
    public void IsRegistered_RegisteredEvent_ReturnsTrue()
    {
        // Arrange
        var metadata = new EventMetadata
        {
            Name = "permission.created",
            Module = "Authorization",
            Category = "Permission"
        };
        
        _registry.RegisterEvent("permission.created", metadata);
        
        // Act
        var result = _registry.IsRegistered("permission.created");
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Fact]
    public void IsRegistered_UnregisteredEvent_ReturnsFalse()
    {
        // Act
        var result = _registry.IsRegistered("nonexistent.event");
        
        // Assert
        result.Should().BeFalse();
    }
    
    [Fact]
    public void IsRegistered_NullName_ReturnsFalse()
    {
        // Act
        var result = _registry.IsRegistered(null!);
        
        // Assert
        result.Should().BeFalse();
    }
    
    [Fact]
    public void GetAllEvents_AfterRegistration_ReturnsAll()
    {
        // Arrange
        _registry.RegisterEvent("permission.created", new EventMetadata
        {
            Name = "permission.created",
            Module = "Authorization",
            Category = "Permission"
        });
        
        _registry.RegisterEvent("role.created", new EventMetadata
        {
            Name = "role.created",
            Module = "Authorization",
            Category = "Role"
        });
        
        // Act
        var result = _registry.GetAllEvents();
        
        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainKey("permission.created");
        result.Should().ContainKey("role.created");
    }
    
    [Fact]
    public void GetAllEvents_EmptyRegistry_ReturnsEmpty()
    {
        // Act
        var result = _registry.GetAllEvents();
        
        // Assert
        result.Should().BeEmpty();
    }
    
    [Fact]
    public void ValidateUniqueness_NoDuplicates_Succeeds()
    {
        // Arrange
        _registry.RegisterEvent("permission.created", new EventMetadata
        {
            Name = "permission.created",
            Module = "Authorization",
            Category = "Permission"
        });
        
        _registry.RegisterEvent("role.created", new EventMetadata
        {
            Name = "role.created",
            Module = "Authorization",
            Category = "Role"
        });
        
        // Act
        var act = () => _registry.ValidateUniqueness();
        
        // Assert
        act.Should().NotThrow();
    }
    
    [Fact]
    public void ValidateUniqueness_InvalidNaming_ThrowsInvalidOperationException()
    {
        // Arrange - Register event with invalid name (uppercase)
        _registry.RegisterEvent("PERMISSION.CREATED", new EventMetadata
        {
            Name = "PERMISSION.CREATED",
            Module = "Authorization",
            Category = "Permission"
        });
        
        // Act
        var act = () => _registry.ValidateUniqueness();
        
        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*naming convention violations*");
    }
    
    [Fact]
    public void ValidateUniqueness_MissingDot_ThrowsInvalidOperationException()
    {
        // Arrange - Register event without dot
        _registry.RegisterEvent("permissioncreated", new EventMetadata
        {
            Name = "permissioncreated",
            Module = "Authorization",
            Category = "Permission"
        });
        
        // Act
        var act = () => _registry.ValidateUniqueness();
        
        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*naming convention violations*");
    }
    
    [Fact]
    public void ValidateUniqueness_CalledTwice_DoesNotThrow()
    {
        // Arrange
        _registry.RegisterEvent("permission.created", new EventMetadata
        {
            Name = "permission.created",
            Module = "Authorization",
            Category = "Permission"
        });
        
        _registry.ValidateUniqueness();
        
        // Act
        var act = () => _registry.ValidateUniqueness();
        
        // Assert
        act.Should().NotThrow("validation should be idempotent");
    }
    
    [Fact]
    public async Task RegisterEvent_ThreadSafety_HandlesCollisions()
    {
        // Arrange
        var tasks = new List<Task>();
        var successCount = 0;
        var failureCount = 0;
        var lockObj = new object();
        
        // Act - Try to register same event from 10 threads
        for (int i = 0; i < 10; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    _registry.RegisterEvent("permission.created", new EventMetadata
                    {
                        Name = "permission.created",
                        Module = $"Module{index}",
                        Category = "Permission"
                    });
                    
                    lock (lockObj)
                    {
                        successCount++;
                    }
                }
                catch (InvalidOperationException)
                {
                    lock (lockObj)
                    {
                        failureCount++;
                    }
                }
            }));
        }
        
        await Task.WhenAll(tasks);
        
        // Assert
        successCount.Should().Be(1, "only one thread should succeed");
        failureCount.Should().Be(9, "nine threads should fail with duplicate error");
        _registry.IsRegistered("permission.created").Should().BeTrue();
    }
}
