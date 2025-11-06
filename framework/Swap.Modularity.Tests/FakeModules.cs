using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using System.Collections.Concurrent;

namespace Swap.Modularity.Tests;

public static class CallLog
{
    public static readonly ConcurrentQueue<string> Entries = new();
    public static void Clear() { while (Entries.TryDequeue(out _)) { } }
}

internal sealed class FakeEndpointRouteBuilder(IServiceProvider services) : IEndpointRouteBuilder
{
    public IServiceProvider ServiceProvider { get; } = services;
    public ICollection<EndpointDataSource> DataSources { get; } = new List<EndpointDataSource>();
    public IApplicationBuilder CreateApplicationBuilder() => throw new NotImplementedException();
}
