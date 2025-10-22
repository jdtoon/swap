using Microsoft.Extensions.DependencyInjection;
using NetMX.Events;
using NetMX.Audit.Web.Events;
using Xunit;

namespace Audit.Web.Tests;

public class DomainEventsIntegrationTests
{
    private readonly IEventRegistry _eventRegistry;

    public DomainEventsIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IEventRegistry, EventRegistry>();
        var serviceProvider = services.BuildServiceProvider();
        _eventRegistry = serviceProvider.GetRequiredService<IEventRegistry>();
        AuditEventDefinitions.Register(_eventRegistry);
    }

    [Fact]
    public void AllAuditEvents_AreRegisteredInRegistry()
    {
        // Just verify that Audit events were registered (at least the ones in AuditEventDefinitions)
        var expectedEvents = new[]
        {
            NetMX.Events.Events.AuditLog.Created,
            NetMX.Events.Events.AuditLog.Viewed,
            NetMX.Events.Events.AuditLog.Exported,
            NetMX.Events.Events.AuditEntry.Recorded,
            NetMX.Events.Events.AuditEntry.Updated,
            NetMX.Events.Events.EntityChange.Tracked,
            NetMX.Events.Events.EntityChange.PropertyChanged,
            NetMX.Events.Events.Compliance.ReportGenerated,
            NetMX.Events.Events.Compliance.ViolationDetected,
            NetMX.Events.Events.Compliance.PolicyUpdated
        };

        foreach (var eventName in expectedEvents)
        {
            Assert.True(_eventRegistry.IsRegistered(eventName), 
                $"Event '{eventName}' should be registered");
        }
        
        var allEvents = _eventRegistry.GetAllEvents();
        var auditEvents = allEvents.Where(e => e.Value.Module == "Audit").ToList();
        Assert.True(auditEvents.Count >= 10, $"Expected at least 10 Audit events, found {auditEvents.Count}");
    }
}
