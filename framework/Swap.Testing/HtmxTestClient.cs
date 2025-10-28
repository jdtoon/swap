using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Headers;

namespace Swap.Testing;

/// <summary>
/// Fluent test client for HTMX-powered ASP.NET Core applications.
/// Provides assertions for partial views, HTMX attributes, and HTML responses.
/// </summary>
public class HtmxTestClient<TProgram> where TProgram : class
{
    private readonly WebApplicationFactory<TProgram> _factory;
    private readonly HttpClient _client;
    private readonly Dictionary<string, string> _defaultHeaders;

    public HtmxTestClient(WebApplicationFactory<TProgram> factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _client = factory.CreateClient();
        _defaultHeaders = new Dictionary<string, string>();
    }

    /// <summary>
    /// Set a default header for all requests.
    /// </summary>
    public HtmxTestClient<TProgram> WithHeader(string name, string value)
    {
        _defaultHeaders[name] = value;
        return this;
    }

    /// <summary>
    /// Set the HX-Request header to simulate HTMX requests.
    /// </summary>
    public HtmxTestClient<TProgram> AsHtmxRequest()
    {
        _defaultHeaders["HX-Request"] = "true";
        return this;
    }

    /// <summary>
    /// Perform an HTTP GET request and return a test response.
    /// </summary>
    public async Task<HtmxTestResponse> GetAsync(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        ApplyDefaultHeaders(request);
        
        var response = await _client.SendAsync(request);
        return new HtmxTestResponse(response);
    }

    /// <summary>
    /// Perform an HTTP POST request and return a test response.
    /// </summary>
    public async Task<HtmxTestResponse> PostAsync(string url, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };
        ApplyDefaultHeaders(request);
        
        var response = await _client.SendAsync(request);
        return new HtmxTestResponse(response);
    }

    /// <summary>
    /// Perform an HTTP PUT request and return a test response.
    /// </summary>
    public async Task<HtmxTestResponse> PutAsync(string url, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = content
        };
        ApplyDefaultHeaders(request);
        
        var response = await _client.SendAsync(request);
        return new HtmxTestResponse(response);
    }

    /// <summary>
    /// Perform an HTTP DELETE request and return a test response.
    /// </summary>
    public async Task<HtmxTestResponse> DeleteAsync(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        ApplyDefaultHeaders(request);
        
        var response = await _client.SendAsync(request);
        return new HtmxTestResponse(response);
    }

    /// <summary>
    /// Perform an HTMX GET request with HX-Request header.
    /// </summary>
    public async Task<HtmxTestResponse> HtmxGetAsync(string url, string? target = null, string? trigger = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("HX-Request", "true");
        
        if (target != null)
            request.Headers.Add("HX-Target", target);
        
        if (trigger != null)
            request.Headers.Add("HX-Trigger", trigger);
        
        ApplyDefaultHeaders(request);
        
        var response = await _client.SendAsync(request);
        return new HtmxTestResponse(response);
    }

    /// <summary>
    /// Perform an HTMX POST request with HX-Request header.
    /// </summary>
    public async Task<HtmxTestResponse> HtmxPostAsync(string url, HttpContent? content = null, string? target = null, string? trigger = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };
        request.Headers.Add("HX-Request", "true");
        
        if (target != null)
            request.Headers.Add("HX-Target", target);
        
        if (trigger != null)
            request.Headers.Add("HX-Trigger", trigger);
        
        ApplyDefaultHeaders(request);
        
        var response = await _client.SendAsync(request);
        return new HtmxTestResponse(response);
    }

    /// <summary>
    /// Convenience overload for HTMX POST with form data.
    /// </summary>
    public Task<HtmxTestResponse> HtmxPostAsync(string url, Dictionary<string, string>? formData, string? target = null, string? trigger = null)
    {
        HttpContent? content = formData != null ? new FormUrlEncodedContent(formData) : null;
        return HtmxPostAsync(url, content, target, trigger);
    }

    /// <summary>
    /// Perform an HTMX PUT request with HX-Request header.
    /// </summary>
    public async Task<HtmxTestResponse> HtmxPutAsync(string url, HttpContent? content = null, string? target = null, string? trigger = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = content
        };
        request.Headers.Add("HX-Request", "true");

        if (target != null)
            request.Headers.Add("HX-Target", target);

        if (trigger != null)
            request.Headers.Add("HX-Trigger", trigger);

        ApplyDefaultHeaders(request);

        var response = await _client.SendAsync(request);
        return new HtmxTestResponse(response);
    }

    /// <summary>
    /// Convenience overload for HTMX PUT with form data.
    /// </summary>
    public Task<HtmxTestResponse> HtmxPutAsync(string url, Dictionary<string, string>? formData, string? target = null, string? trigger = null)
    {
        HttpContent? content = formData != null ? new FormUrlEncodedContent(formData) : null;
        return HtmxPutAsync(url, content, target, trigger);
    }

    /// <summary>
    /// Perform an HTMX DELETE request with HX-Request header.
    /// </summary>
    public async Task<HtmxTestResponse> HtmxDeleteAsync(string url, string? target = null, string? trigger = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        request.Headers.Add("HX-Request", "true");

        if (target != null)
            request.Headers.Add("HX-Target", target);

        if (trigger != null)
            request.Headers.Add("HX-Trigger", trigger);

        ApplyDefaultHeaders(request);

        var response = await _client.SendAsync(request);
        return new HtmxTestResponse(response);
    }

    /// <summary>
    /// Convenience overload for POST with form data.
    /// </summary>
    public Task<HtmxTestResponse> PostAsync(string url, Dictionary<string, string>? formData)
    {
        HttpContent? content = formData != null ? new FormUrlEncodedContent(formData) : null;
        return PostAsync(url, content);
    }

    /// <summary>
    /// Convenience overload for PUT with form data.
    /// </summary>
    public Task<HtmxTestResponse> PutAsync(string url, Dictionary<string, string>? formData)
    {
        HttpContent? content = formData != null ? new FormUrlEncodedContent(formData) : null;
        return PutAsync(url, content);
    }

    private void ApplyDefaultHeaders(HttpRequestMessage request)
    {
        foreach (var (key, value) in _defaultHeaders)
        {
            request.Headers.TryAddWithoutValidation(key, value);
        }
    }
}
