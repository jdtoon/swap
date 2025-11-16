using Microsoft.AspNetCore.Mvc.Testing;

namespace Swap.Testing;

/// <summary>
/// Base class for HTMX integration tests providing test client setup.
/// </summary>
/// <typeparam name="TProgram">The entry point of your ASP.NET Core application.</typeparam>
public class HtmxTestFixture<TProgram> : IDisposable where TProgram : class
{
    private readonly WebApplicationFactory<TProgram> _factory;
    private readonly HtmxTestClient<TProgram> _client;

    public HtmxTestFixture()
    {
        _factory = new WebApplicationFactory<TProgram>();
        _client = new HtmxTestClient<TProgram>(_factory);
    }

    /// <summary>
    /// Gets the test client for making requests.
    /// </summary>
    public HtmxTestClient<TProgram> Client => _client;

    /// <summary>
    /// Gets the web application factory for advanced configuration.
    /// </summary>
    public WebApplicationFactory<TProgram> Factory => _factory;

    public void Dispose()
    {
        _factory?.Dispose();
    }
}
