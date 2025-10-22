using Microsoft.Extensions.DependencyInjection;
using NetMX.Events;
using NetMX.Identity.Web.Events;
using Xunit;

namespace NetMX.Identity.Web.Tests;

public class DomainEventsIntegrationTests
{
    private readonly IEventRegistry _eventRegistry;

    public DomainEventsIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IEventRegistry, EventRegistry>();
        var serviceProvider = services.BuildServiceProvider();
        _eventRegistry = serviceProvider.GetRequiredService<IEventRegistry>();
        IdentityEventDefinitions.Register(_eventRegistry);
    }

    [Fact]
    public void AllIdentityEvents_AreRegisteredInRegistry()
    {
        var expectedEvents = new[]
        {
            NetMX.Events.Events.User.Registered,
            NetMX.Events.Events.User.ProfileUpdated,
            NetMX.Events.Events.User.EmailConfirmed,
            NetMX.Events.Events.User.Deleted,
            NetMX.Events.Events.User.Login.Success,
            NetMX.Events.Events.User.Login.Failed,
            NetMX.Events.Events.User.Login.LockedOut,
            NetMX.Events.Events.User.Login.TwoFactorRequired,
            NetMX.Events.Events.User.Session.Created,
            NetMX.Events.Events.User.Session.Renewed,
            NetMX.Events.Events.User.Session.Expired,
            NetMX.Events.Events.User.Password.Changed,
            NetMX.Events.Events.User.Password.ResetRequested,
            NetMX.Events.Events.User.Password.ResetCompleted,
            NetMX.Events.Events.User.Account.Locked,
            NetMX.Events.Events.User.Account.Unlocked
        };

        foreach (var eventName in expectedEvents)
        {
            Assert.True(_eventRegistry.IsRegistered(eventName));
        }
        
        var allEvents = _eventRegistry.GetAllEvents();
        var identityEvents = allEvents.Where(e => e.Value.Module == "Identity").ToList();
        Assert.Equal(16, identityEvents.Count);
    }
}
