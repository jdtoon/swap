using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Swap.Modularity.Hosting;
using Xunit;

namespace Swap.Modularity.Tests;

public class ModularityTests
{
    private static IConfiguration EmptyConfig() => new ConfigurationBuilder().Build();

    [Fact]
    public void AddSwapModules_OrdersByDependencies_AndCallsConfigureServices()
    {
        // Arrange
        CallLog.Clear();
        var services = new ServiceCollection();
        var asm = ModuleAssemblyBuilder.Build(
            new ModuleAssemblyBuilder.ModuleSpec("A", Array.Empty<string>()),
            new ModuleAssemblyBuilder.ModuleSpec("B", new[] { "A" }),
            new ModuleAssemblyBuilder.ModuleSpec("C", new[] { "B" })
        );

        // Act
        services.AddSwapModules(EmptyConfig(), new[] { asm });
    using var provider = services.BuildServiceProvider();

    // Assert ConfigureServices called for each in topological order A -> B -> C
    Assert.Equal(new[] { "services:A", "services:B", "services:C" }, CallLog.Entries.ToArray());
    }

    [Fact]
    public void MapSwapModuleEndpoints_CallsInTopologicalOrder()
    {
        // Arrange
        CallLog.Clear();
        var services = new ServiceCollection();
        var asm = ModuleAssemblyBuilder.Build(
            new ModuleAssemblyBuilder.ModuleSpec("A", Array.Empty<string>()),
            new ModuleAssemblyBuilder.ModuleSpec("B", new[] { "A" }),
            new ModuleAssemblyBuilder.ModuleSpec("C", new[] { "B" })
        );
        services.AddSwapModules(EmptyConfig(), new[] { asm });
        using var provider = services.BuildServiceProvider();
        var endpoints = new FakeEndpointRouteBuilder(provider);

        // Act
        endpoints.MapSwapModuleEndpoints();

    // Assert endpoint call order only
    var endpointsOrder = CallLog.Entries.Where(x => x.StartsWith("endpoints:")).ToArray();
    Assert.Equal(new[] { "endpoints:A", "endpoints:B", "endpoints:C" }, endpointsOrder);
    }

    [Fact]
    public void AddSwapModules_ThrowsOnMissingDependency()
    {
        var services = new ServiceCollection();
        var asm = ModuleAssemblyBuilder.Build(
            new ModuleAssemblyBuilder.ModuleSpec("MissingDep", new[] { "DoesNotExist" })
        );
        var ex = Assert.Throws<InvalidOperationException>(() => services.AddSwapModules(EmptyConfig(), new[] { asm }));
        Assert.Contains("depends on missing module", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddSwapModules_ThrowsOnDependencyCycle()
    {
        var services = new ServiceCollection();
        var asm = ModuleAssemblyBuilder.Build(
            new ModuleAssemblyBuilder.ModuleSpec("X", new[] { "Y" }, LogCalls: false),
            new ModuleAssemblyBuilder.ModuleSpec("Y", new[] { "X" }, LogCalls: false)
        );
        var ex = Assert.Throws<InvalidOperationException>(() => services.AddSwapModules(EmptyConfig(), new[] { asm }));
        Assert.Contains("cycle", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
