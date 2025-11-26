using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Swap.Htmx.TagHelpers;

/// <summary>
/// Tag helper that renders the Swap.Htmx DevTools script in development environments.
/// Use this tag helper in your layout to automatically include debugging tools during development.
/// </summary>
/// <example>
/// <code>
/// &lt;!-- In _Layout.cshtml --&gt;
/// &lt;script src="~/_content/Swap.Htmx/js/swap.client.js"&gt;&lt;/script&gt;
/// &lt;swap-devtools /&gt;
/// </code>
/// </example>
[HtmlTargetElement("swap-devtools", TagStructure = TagStructure.WithoutEndTag)]
public class SwapDevToolsTagHelper : TagHelper
{
    private readonly IWebHostEnvironment _env;
    private readonly SwapHtmxOptions _options;

    public SwapDevToolsTagHelper(IWebHostEnvironment env, SwapHtmxOptions options)
    {
        _env = env;
        _options = options;
    }

    /// <summary>
    /// Forces the DevTools to be included regardless of environment.
    /// Default: false (only included in Development).
    /// </summary>
    public bool Force { get; set; } = false;

    /// <summary>
    /// Automatically show the DevTools panel on page load.
    /// Default: false.
    /// </summary>
    public bool ShowPanel { get; set; } = false;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        // Only render in development or if forced
        var shouldRender = Force || 
                          _env.IsDevelopment() || 
                          _options.Diagnostics.EnableClientLogging ||
                          _options.Diagnostics.EnableDevToolsPanel;

        if (!shouldRender)
        {
            output.SuppressOutput();
            return;
        }

        output.TagName = null; // Don't render any wrapper tag
        
        var autoShowPanel = ShowPanel || _options.Diagnostics.EnableDevToolsPanel;
        
        output.Content.SetHtmlContent($@"
<script src=""/_content/Swap.Htmx/js/swap.devtools.js""></script>
{(autoShowPanel ? @"<script>
    document.addEventListener('DOMContentLoaded', function() {{
        if (window.Swap && window.Swap.DevTools) {{
            Swap.DevTools.showPanel();
        }}
    }});
</script>" : "")}
");
    }
}

/// <summary>
/// Tag helper that renders the Swap.Htmx state container debug panel.
/// Shows current state values in a collapsible panel during development.
/// </summary>
/// <example>
/// <code>
/// &lt;swap-state-debug container-id="inventory-state" /&gt;
/// </code>
/// </example>
[HtmlTargetElement("swap-state-debug", TagStructure = TagStructure.WithoutEndTag)]
public class SwapStateDebugTagHelper : TagHelper
{
    private readonly IWebHostEnvironment _env;

    public SwapStateDebugTagHelper(IWebHostEnvironment env)
    {
        _env = env;
    }

    /// <summary>
    /// The ID of the state container to debug.
    /// </summary>
    public string? ContainerId { get; set; }

    /// <summary>
    /// Whether to start in expanded state.
    /// </summary>
    public bool Expanded { get; set; } = false;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (!_env.IsDevelopment())
        {
            output.SuppressOutput();
            return;
        }

        output.TagName = "details";
        output.Attributes.SetAttribute("class", "swap-state-debug");
        output.Attributes.SetAttribute("style", "font-family: monospace; font-size: 12px; background: #f5f5f5; padding: 8px; margin: 8px 0; border-radius: 4px;");
        
        if (Expanded)
        {
            output.Attributes.SetAttribute("open", "open");
        }

        var containerId = ContainerId ?? "state";
        
        output.Content.SetHtmlContent($@"
<summary style=""cursor: pointer; font-weight: bold; color: #666;"">🔍 State Debug: #{containerId}</summary>
<pre id=""{containerId}-debug"" style=""margin: 8px 0 0 0; white-space: pre-wrap;""></pre>
<script>
(function() {{
    function updateDebug() {{
        var container = document.getElementById('{containerId}');
        var debugEl = document.getElementById('{containerId}-debug');
        if (!container || !debugEl) return;
        
        var state = {{}};
        container.querySelectorAll('input[type=""hidden""]').forEach(function(input) {{
            state[input.name || input.id] = input.value;
        }});
        debugEl.textContent = JSON.stringify(state, null, 2);
    }}
    
    updateDebug();
    document.body.addEventListener('htmx:afterSwap', updateDebug);
    
    // Watch for value changes
    var container = document.getElementById('{containerId}');
    if (container && window.Swap && window.Swap.DevTools) {{
        Swap.DevTools.watchState('{containerId}');
    }}
}})();
</script>
");
    }
}
