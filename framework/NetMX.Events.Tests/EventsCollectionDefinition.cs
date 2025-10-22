using Xunit;

namespace NetMX.Events.Tests;

/// <summary>
/// Collection definition to prevent parallel test execution for Events static class tests.
/// This ensures Events.Initialize() is not called concurrently.
/// </summary>
[CollectionDefinition("EventsCollection", DisableParallelization = true)]
public class EventsCollectionDefinition
{
}
