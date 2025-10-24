using Microsoft.Extensions.DependencyInjection;
using NetMX.DependencyInjection;
using Xunit;

namespace NetMX.Core.Tests.DependencyInjection;

public class ServiceCollectionExtensionsTests
{
    #region Test Services

    public interface ITestScopedService
    {
        string GetValue();
    }

    public class TestScopedService : ITestScopedService, IScopedDependency
    {
        public string GetValue() => "Scoped";
    }

    public interface ITestTransientService
    {
        string GetValue();
    }

    public class TestTransientService : ITestTransientService, ITransientDependency
    {
        public string GetValue() => "Transient";
    }

    public interface ITestSingletonService
    {
        string GetValue();
    }

    public class TestSingletonService : ITestSingletonService, ISingletonDependency
    {
        public string GetValue() => "Singleton";
    }

    public class TestServiceWithoutInterface : IScopedDependency
    {
        public string GetValue() => "NoInterface";
    }

    public interface IMultiInterfaceService
    {
        string GetValue();
    }

    public interface IOtherInterface
    {
        int GetNumber();
    }

    public class MultiInterfaceService : IMultiInterfaceService, IOtherInterface, IScopedDependency
    {
        public string GetValue() => "Multi";
        public int GetNumber() => 42;
    }

    #endregion

    [Fact]
    public void AddNetMXServices_RegistersScopedServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNetMXServices(typeof(TestScopedService).Assembly);

        // Assert
        var provider = services.BuildServiceProvider();
        var service = provider.GetService<ITestScopedService>();

        Assert.NotNull(service);
        Assert.IsType<TestScopedService>(service);
        Assert.Equal("Scoped", service.GetValue());
    }

    [Fact]
    public void AddNetMXServices_RegistersTransientServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNetMXServices(typeof(TestTransientService).Assembly);

        // Assert
        var provider = services.BuildServiceProvider();
        var service1 = provider.GetService<ITestTransientService>();
        var service2 = provider.GetService<ITestTransientService>();

        Assert.NotNull(service1);
        Assert.NotNull(service2);
        Assert.NotSame(service1, service2); // Transient = new instance each time
    }

    [Fact]
    public void AddNetMXServices_RegistersSingletonServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNetMXServices(typeof(TestSingletonService).Assembly);

        // Assert
        var provider = services.BuildServiceProvider();
        var service1 = provider.GetService<ITestSingletonService>();
        var service2 = provider.GetService<ITestSingletonService>();

        Assert.NotNull(service1);
        Assert.NotNull(service2);
        Assert.Same(service1, service2); // Singleton = same instance
    }

    [Fact]
    public void AddNetMXServices_RegistersServiceWithoutInterface_AsSelf()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNetMXServices(typeof(TestServiceWithoutInterface).Assembly);

        // Assert
        var provider = services.BuildServiceProvider();
        var service = provider.GetService<TestServiceWithoutInterface>();

        Assert.NotNull(service);
        Assert.Equal("NoInterface", service.GetValue());
    }

    [Fact]
    public void AddNetMXServices_RegistersServiceWithMultipleInterfaces()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNetMXServices(typeof(MultiInterfaceService).Assembly);

        // Assert
        var provider = services.BuildServiceProvider();
        var service1 = provider.GetService<IMultiInterfaceService>();
        var service2 = provider.GetService<IOtherInterface>();

        Assert.NotNull(service1);
        Assert.NotNull(service2);
        Assert.IsType<MultiInterfaceService>(service1);
        Assert.IsType<MultiInterfaceService>(service2);
        
        // Note: Each interface gets its own registration, so instances are different
        // This is expected behavior for scoped services in different resolution contexts
    }

    [Fact]
    public void AddNetMXServices_ThrowsException_WhenNoAssembliesProvided()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => services.AddNetMXServices());
    }

    [Fact]
    public void AddNetMXServices_HandlesMultipleAssemblies()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly1 = typeof(TestScopedService).Assembly;
        var assembly2 = typeof(ServiceCollectionExtensions).Assembly;

        // Act
        services.AddNetMXServices(assembly1, assembly2);

        // Assert
        var provider = services.BuildServiceProvider();
        var service = provider.GetService<ITestScopedService>();

        Assert.NotNull(service);
    }

    [Fact]
    public void AddNetMXServicesFromTypes_RegistersServicesFromTypeAssemblies()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNetMXServicesFromTypes(typeof(TestScopedService), typeof(TestTransientService));

        // Assert
        var provider = services.BuildServiceProvider();
        var scopedService = provider.GetService<ITestScopedService>();
        var transientService = provider.GetService<ITestTransientService>();

        Assert.NotNull(scopedService);
        Assert.NotNull(transientService);
    }

    [Fact]
    public void AddNetMXServicesFromTypes_ThrowsException_WhenNoTypesProvided()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => services.AddNetMXServicesFromTypes());
    }

    [Fact]
    public void AddNetMXServices_DoesNotRegisterAbstractClasses()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNetMXServices(typeof(AbstractTestService).Assembly);

        // Assert
        var provider = services.BuildServiceProvider();
        var service = provider.GetService<IAbstractTestService>();

        Assert.Null(service); // Abstract class should not be registered
    }

    [Fact]
    public void AddNetMXServices_DoesNotRegisterInterfaces()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNetMXServices(typeof(ITestScopedService).Assembly);

        // Assert
        var provider = services.BuildServiceProvider();
        
        // Should be registered via implementation, not interface directly
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ITestScopedService));
        Assert.NotNull(descriptor);
        Assert.Equal(typeof(TestScopedService), descriptor.ImplementationType);
    }

    [Fact]
    public void AddNetMXServices_AvoidsDuplicateRegistrations()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNetMXServices(typeof(TestScopedService).Assembly);
        services.AddNetMXServices(typeof(TestScopedService).Assembly); // Register again

        // Assert
        var descriptors = services.Where(s => s.ServiceType == typeof(ITestScopedService)).ToList();
        
        Assert.Single(descriptors); // Should only be registered once
    }

    #region Abstract Test Services

    public interface IAbstractTestService
    {
        string GetValue();
    }

    public abstract class AbstractTestService : IAbstractTestService, IScopedDependency
    {
        public abstract string GetValue();
    }

    #endregion
}
