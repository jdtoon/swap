using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Headers;
using AngleSharp;
using AngleSharp.Html.Dom;

namespace Swap.Testing;

/// <summary>
/// Fluent test client for HTMX-powered ASP.NET Core applications.
/// Wraps <see cref="WebApplicationFactory{TProgram}"/> and exposes
/// HTMX-aware helpers (for example <c>HtmxGetAsync</c>, <c>HtmxPostAsync</c>)
/// that automatically set <c>HX-Request</c>, <c>HX-Target</c> and
/// <c>HX-Trigger</c> headers and return <see cref="HtmxTestResponse"/>.
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
    /// Set a default header for all subsequent requests created by this
    /// client. This is useful for things like authentication headers.
    /// </summary>
    public HtmxTestClient<TProgram> WithHeader(string name, string value)
    {
        _defaultHeaders[name] = value;
        return this;
    }

    /// <summary>
    /// Set the <c>HX-Request</c> header for all subsequent requests so they
    /// are treated as HTMX requests by the application under test.
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
    /// Perform an HTMX GET request with <c>HX-Request</c> header and optional
    /// <c>HX-Target</c>/<c>HX-Trigger</c> values.
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
    /// Perform an HTMX POST request with <c>HX-Request</c> header and
    /// optional <c>HX-Target</c>/<c>HX-Trigger</c> values.
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
    /// Convenience overload for HTMX POST with URL-encoded form data.
    /// </summary>
    public Task<HtmxTestResponse> HtmxPostAsync(string url, Dictionary<string, string>? formData, string? target = null, string? trigger = null)
    {
        HttpContent? content = formData != null ? new FormUrlEncodedContent(formData) : null;
        return HtmxPostAsync(url, content, target, trigger);
    }

    /// <summary>
    /// Perform an HTMX PUT request with <c>HX-Request</c> header.
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
    /// Convenience overload for HTMX PUT with URL-encoded form data.
    /// </summary>
    public Task<HtmxTestResponse> HtmxPutAsync(string url, Dictionary<string, string>? formData, string? target = null, string? trigger = null)
    {
        HttpContent? content = formData != null ? new FormUrlEncodedContent(formData) : null;
        return HtmxPutAsync(url, content, target, trigger);
    }

    /// <summary>
    /// Perform an HTMX DELETE request with <c>HX-Request</c> header.
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
    /// Convenience overload for POST with URL-encoded form data.
    /// </summary>
    public Task<HtmxTestResponse> PostAsync(string url, Dictionary<string, string>? formData)
    {
        HttpContent? content = formData != null ? new FormUrlEncodedContent(formData) : null;
        return PostAsync(url, content);
    }

    /// <summary>
    /// Convenience overload for PUT with URL-encoded form data.
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

    /// <summary>
    /// Submit a form found in a previous HTML response. Automatically detects hx-* attributes (hx-post/get/put/delete),
    /// action and method, gathers input fields, merges overrides, and submits as an HTMX request.
    /// </summary>
    /// <param name="response">The prior response containing the form</param>
    /// <param name="formSelector">CSS selector for the form element</param>
    /// <param name="overrides">Optional name/value overrides to submit</param>
    /// <param name="target">Optional HX-Target header override</param>
    /// <param name="trigger">Optional HX-Trigger header override</param>
    public async Task<HtmxTestResponse> SubmitFormAsync(HtmxTestResponse response, string formSelector, Dictionary<string, string>? overrides = null, string? target = null, string? trigger = null)
    {
        if (response == null) throw new ArgumentNullException(nameof(response));
        if (string.IsNullOrWhiteSpace(formSelector)) throw new ArgumentException("Form selector is required", nameof(formSelector));

        var doc = await response.GetDocumentAsync();
        var form = doc.QuerySelector(formSelector) as IHtmlFormElement ?? throw new HtmxTestException($"Form '{formSelector}' not found.");

        // Determine URL and method, preferring hx-* attributes
        string? url = form.GetAttribute("hx-post") ?? form.GetAttribute("hx-put") ?? form.GetAttribute("hx-delete") ?? form.GetAttribute("hx-get") ?? form.Action;
        if (string.IsNullOrWhiteSpace(url))
            throw new HtmxTestException("Form action/hx-* attribute not found; cannot determine submission URL.");

        HttpMethod method;
        if (form.HasAttribute("hx-post")) method = HttpMethod.Post;
        else if (form.HasAttribute("hx-put")) method = HttpMethod.Put;
        else if (form.HasAttribute("hx-delete")) method = HttpMethod.Delete;
        else if (form.HasAttribute("hx-get")) method = HttpMethod.Get;
        else
            method = new HttpMethod((form.Method ?? "GET").ToUpperInvariant());

        // Collect form fields
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var input in form.QuerySelectorAll("input").OfType<IHtmlInputElement>())
        {
            if (string.IsNullOrWhiteSpace(input.Name) || input.IsDisabled) continue;
            var type = (input.Type ?? "text").ToLowerInvariant();
            if (type is "checkbox" or "radio")
            {
                if (input.IsChecked)
                {
                    fields[input.Name] = input.Value ?? "on";
                }
                continue;
            }
            fields[input.Name] = input.Value ?? string.Empty;
        }

        foreach (var ta in form.QuerySelectorAll("textarea").OfType<IHtmlTextAreaElement>())
        {
            if (string.IsNullOrWhiteSpace(ta.Name) || ta.IsDisabled) continue;
            fields[ta.Name] = ta.Value ?? ta.TextContent ?? string.Empty;
        }

        foreach (var sel in form.QuerySelectorAll("select").OfType<IHtmlSelectElement>())
        {
            if (string.IsNullOrWhiteSpace(sel.Name) || sel.IsDisabled) continue;
            if (sel.IsMultiple)
            {
                var selected = sel.Options.Where(o => o.IsSelected).Select(o => o.Value ?? o.TextContent).ToArray();
                if (selected.Length > 0) fields[sel.Name] = string.Join(",", selected);
            }
            else
            {
                var opt = sel.Options.FirstOrDefault(o => o.IsSelected) ?? sel.Options.FirstOrDefault();
                if (opt != null) fields[sel.Name] = opt.Value ?? opt.TextContent ?? string.Empty;
            }
        }

        // Apply overrides
        if (overrides != null)
        {
            foreach (var kvp in overrides)
            {
                fields[kvp.Key] = kvp.Value;
            }
        }

        // Headers: HX-Request + optional target/trigger (prefer explicit params, else form attrs)
        var effectiveTarget = target ?? form.GetAttribute("hx-target");
        var effectiveTrigger = trigger ?? form.GetAttribute("hx-trigger");

        if (method == HttpMethod.Get)
        {
            // Append query string
            if (fields.Count > 0)
            {
                var query = string.Join("&", fields.Select(kv => Uri.EscapeDataString(kv.Key) + "=" + Uri.EscapeDataString(kv.Value)));
                url = url.Contains("?") ? url + "&" + query : url + "?" + query;
            }
            return await HtmxGetAsync(url, effectiveTarget, effectiveTrigger);
        }
        else if (method == HttpMethod.Post)
        {
            var content = new FormUrlEncodedContent(fields);
            return await HtmxPostAsync(url, content, effectiveTarget, effectiveTrigger);
        }
        else if (method == HttpMethod.Put)
        {
            var content = new FormUrlEncodedContent(fields);
            return await HtmxPutAsync(url, content, effectiveTarget, effectiveTrigger);
        }
        else if (method == HttpMethod.Delete)
        {
            return await HtmxDeleteAsync(url, effectiveTarget, effectiveTrigger);
        }
        else
        {
            // Fallback: send as POST
            var content = new FormUrlEncodedContent(fields);
            return await HtmxPostAsync(url, content, effectiveTarget, effectiveTrigger);
        }
    }

    /// <summary>
    /// Perform an HTMX GET request to a specific Razor Page handler.
    /// Automatically appends the ?handler=... query parameter.
    /// </summary>
    /// <param name="pageName">The path to the page (e.g. "/Index").</param>
    /// <param name="handler">The name of the handler method (without OnGet/OnPost prefix).</param>
    /// <param name="routeValues">Optional object containing route values to be added to the query string.</param>
    /// <param name="target">Optional HX-Target header value.</param>
    /// <param name="trigger">Optional HX-Trigger header value.</param>
    public Task<HtmxTestResponse> HtmxGetPageHandlerAsync(string pageName, string handler, object? routeValues = null, string? target = null, string? trigger = null)
    {
        var url = pageName;
        var queryParams = new Dictionary<string, string>();
        
        if (routeValues != null)
        {
            foreach (var prop in routeValues.GetType().GetProperties())
            {
                var value = prop.GetValue(routeValues)?.ToString();
                if (value != null)
                {
                    queryParams[prop.Name] = value;
                }
            }
        }
        
        // Add handler
        queryParams["handler"] = handler;
        
        var queryString = string.Join("&", queryParams.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
        url = url.Contains("?") ? $"{url}&{queryString}" : $"{url}?{queryString}";
        
        return HtmxGetAsync(url, target, trigger);
    }

    /// <summary>
    /// Perform an HTMX POST request to a specific Razor Page handler.
    /// Automatically appends the ?handler=... query parameter.
    /// </summary>
    /// <param name="pageName">The path to the page (e.g. "/Index").</param>
    /// <param name="handler">The name of the handler method (without OnGet/OnPost prefix).</param>
    /// <param name="formValues">Optional object containing form values to be sent in the body.</param>
    /// <param name="target">Optional HX-Target header value.</param>
    /// <param name="trigger">Optional HX-Trigger header value.</param>
    public Task<HtmxTestResponse> HtmxPostPageHandlerAsync(string pageName, string handler, object? formValues = null, string? target = null, string? trigger = null)
    {
        var url = pageName;
        if (url.Contains("?"))
            url += $"&handler={handler}";
        else
            url += $"?handler={handler}";
            
        HttpContent? content = null;
        if (formValues != null)
        {
            var dict = new Dictionary<string, string>();
            foreach (var prop in formValues.GetType().GetProperties())
            {
                var value = prop.GetValue(formValues)?.ToString();
                if (value != null)
                {
                    dict[prop.Name] = value;
                }
            }
            content = new FormUrlEncodedContent(dict);
        }
        
        return HtmxPostAsync(url, content, target, trigger);
    }

    /// <summary>
    /// If a previous response returned an <c>HX-Redirect</c> header, follow
    /// it and return the fetched response (performed as an HTMX GET).
    /// </summary>
    public async Task<HtmxTestResponse> FollowHxRedirectAsync(HtmxTestResponse response, string? target = null, string? trigger = null)
    {
        if (response == null) throw new ArgumentNullException(nameof(response));
        var url = response.GetHxRedirectUrl() ?? throw new HtmxTestException("HX-Redirect header not present on response.");
        return await HtmxGetAsync(url, target, trigger);
    }
}
