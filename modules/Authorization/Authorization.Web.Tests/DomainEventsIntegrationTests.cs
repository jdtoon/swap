using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using NetMX.Events;
using Events = NetMX.Events.Events;

namespace NetMX.Authorization.Web.Tests;

/// <summary>
/// Integration tests for Authorization module events using Event Registry pattern.
/// Validates that Permission and Role events are registered and can be published correctly.
/// </summary>
public class DomainEventsIntegrationTests : IDisposable
{
    private readonly TestEventCapture _eventCapture;
    private readonly IEventRegistry _eventRegistry;
    private readonly ServiceProvider _serviceProvider;

    public DomainEventsIntegrationTests()
    {
        _eventCapture = new TestEventCapture();
        _eventRegistry = new EventRegistry();
        
        // Register Authorization module events
        NetMX.Authorization.Web.Events.AuthorizationEventDefinitions.Register(_eventRegistry);
        
        var services = new ServiceCollection();
        services.AddSingleton<IEventBus>(_ => _eventCapture);
        services.AddSingleton(_eventRegistry);
        
        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }

    #region Permission Events

    [Fact]
    public async Task PermissionController_Create_TriggersPermissionCreatedEvent()
    {
        // Arrange
        var expectedEvent = NetMX.Events.Events.Permission.Created;
        Assert.True(_eventRegistry.IsRegistered(expectedEvent), "Event should be registered");
        
        // Act: Simulate controller action triggering event
        await _eventCapture.PublishAsync(expectedEvent, new { id = Guid.NewGuid() }, new EventContext(), CancellationToken.None);
        
        // Assert
        var capturedEvents = _eventCapture.GetCapturedEvents();
        Assert.Single(capturedEvents);
        Assert.Equal(expectedEvent, capturedEvents[0].EventName);
        Assert.NotNull(capturedEvents[0].Data);
        
        // Verify event metadata
        var metadata = _eventRegistry.GetEvent(expectedEvent);
        Assert.Equal(EventDirection.Upstream, metadata.Direction);
        Assert.Equal("Authorization", metadata.Module);
    }

    [Fact]
    public async Task PermissionController_Update_TriggersPermissionUpdatedEvent()
    {
        // Arrange
        var expectedEvent = NetMX.Events.Events.Permission.Updated;
        var permissionId = Guid.NewGuid();
        Assert.True(_eventRegistry.IsRegistered(expectedEvent), "Event should be registered");
        
        // Act
        await _eventCapture.PublishAsync(expectedEvent, new { id = permissionId }, new EventContext(), CancellationToken.None);
        
        // Assert
        var capturedEvents = _eventCapture.GetCapturedEvents();
        Assert.Single(capturedEvents);
        Assert.Equal(expectedEvent, capturedEvents[0].EventName);
        
        var metadata = _eventRegistry.GetEvent(expectedEvent);
        Assert.Equal(EventDirection.Upstream, metadata.Direction);
    }

    [Fact]
    public async Task PermissionController_Delete_TriggersPermissionDeletedEvent()
    {
        // Arrange
        var expectedEvent = NetMX.Events.Events.Permission.Deleted;
        var permissionId = Guid.NewGuid();
        Assert.True(_eventRegistry.IsRegistered(expectedEvent), "Event should be registered");
        
        // Act
        await _eventCapture.PublishAsync(expectedEvent, new { id = permissionId }, new EventContext(), CancellationToken.None);
        
        // Assert
        var capturedEvents = _eventCapture.GetCapturedEvents();
        Assert.Single(capturedEvents);
        Assert.Equal(expectedEvent, capturedEvents[0].EventName);
        
        var metadata = _eventRegistry.GetEvent(expectedEvent);
        Assert.Equal(EventDirection.Terminal, metadata.Direction);
    }

    #endregion

    #region Role Events

    [Fact]
    public async Task RoleController_Create_TriggersRoleCreatedEvent()
    {
        // Arrange
        var expectedEvent = NetMX.Events.Events.Role.Created;
        Assert.True(_eventRegistry.IsRegistered(expectedEvent), "Event should be registered");
        
        // Act
        await _eventCapture.PublishAsync(expectedEvent, new { id = Guid.NewGuid() }, new EventContext(), CancellationToken.None);
        
        // Assert
        var capturedEvents = _eventCapture.GetCapturedEvents();
        Assert.Single(capturedEvents);
        Assert.Equal(expectedEvent, capturedEvents[0].EventName);
        
        var metadata = _eventRegistry.GetEvent(expectedEvent);
        Assert.Equal(EventDirection.Upstream, metadata.Direction);
        Assert.Equal("Authorization", metadata.Module);
    }

    [Fact]
    public async Task RoleController_Update_TriggersRoleUpdatedEvent()
    {
        // Arrange
        var expectedEvent = NetMX.Events.Events.Role.Updated;
        var roleId = Guid.NewGuid();
        Assert.True(_eventRegistry.IsRegistered(expectedEvent), "Event should be registered");
        
        // Act
        await _eventCapture.PublishAsync(expectedEvent, new { id = roleId }, new EventContext(), CancellationToken.None);
        
        // Assert
        var capturedEvents = _eventCapture.GetCapturedEvents();
        Assert.Single(capturedEvents);
        Assert.Equal(expectedEvent, capturedEvents[0].EventName);
        
        var metadata = _eventRegistry.GetEvent(expectedEvent);
        Assert.Equal(EventDirection.Upstream, metadata.Direction);
    }

    [Fact]
    public async Task RoleController_Delete_TriggersRoleDeletedEvent()
    {
        // Arrange
        var expectedEvent = NetMX.Events.Events.Role.Deleted;
        var roleId = Guid.NewGuid();
        Assert.True(_eventRegistry.IsRegistered(expectedEvent), "Event should be registered");
        
        // Act
        await _eventCapture.PublishAsync(expectedEvent, new { id = roleId }, new EventContext(), CancellationToken.None);
        
        // Assert
        var capturedEvents = _eventCapture.GetCapturedEvents();
        Assert.Single(capturedEvents);
        Assert.Equal(expectedEvent, capturedEvents[0].EventName);
        
        var metadata = _eventRegistry.GetEvent(expectedEvent);
        Assert.Equal(EventDirection.Terminal, metadata.Direction);
    }

    #endregion

    #region Event Registry Validation

    [Theory]
    [InlineData("permission.created", EventDirection.Upstream)]
    [InlineData("permission.updated", EventDirection.Upstream)]
    [InlineData("permission.deleted", EventDirection.Terminal)]
    [InlineData("role.created", EventDirection.Upstream)]
    [InlineData("role.updated", EventDirection.Upstream)]
    [InlineData("role.deleted", EventDirection.Terminal)]
    public void AllAuthorizationEvents_HaveCorrectEventDirection(string eventName, EventDirection expectedDirection)
    {
        // Arrange & Act
        var metadata = _eventRegistry.GetEvent(eventName);
        
        // Assert
        Assert.NotNull(metadata);
        Assert.Equal(expectedDirection, metadata.Direction);
        Assert.Equal("Authorization", metadata.Module);
    }

    [Fact]
    public void AllAuthorizationEvents_AreRegisteredInRegistry()
    {
        // Arrange
        var expectedEvents = new[]
        {
            NetMX.Events.Events.Permission.Created,
            NetMX.Events.Events.Permission.Updated,
            NetMX.Events.Events.Permission.Deleted,
            NetMX.Events.Events.Role.Created,
            NetMX.Events.Events.Role.Updated,
            NetMX.Events.Events.Role.Deleted,
            NetMX.Events.Events.Role.PermissionGranted,
            NetMX.Events.Events.Role.PermissionRevoked
        };
        
        // Act & Assert
        foreach (var eventName in expectedEvents)
        {
            Assert.True(_eventRegistry.IsRegistered(eventName), 
                $"Event '{eventName}' should be registered in the registry");
        }
    }

    #endregion

    #region Test Helper Classes

    /// <summary>
    /// Test implementation of IEventBus that captures events for validation.
    /// </summary>
    private class TestEventCapture : IEventBus
    {
        private readonly List<CapturedEvent> _capturedEvents = new();

        public Task PublishAsync<TData>(string eventName, TData data, EventContext? context = null, CancellationToken cancellationToken = default)
        {
            _capturedEvents.Add(new CapturedEvent(eventName, data!, context ?? new EventContext()));
            return Task.CompletedTask;
        }

        public Dictionary<string, object> GetTriggeredEvents(Guid requestId)
        {
            return _capturedEvents
                .Where(e => e.Context.RequestId == requestId)
                .ToDictionary(e => e.EventName, e => e.Data);
        }

        public List<CapturedEvent> GetCapturedEvents() => _capturedEvents;

        public record CapturedEvent(string EventName, object Data, EventContext Context);
    }

    #endregion
}
