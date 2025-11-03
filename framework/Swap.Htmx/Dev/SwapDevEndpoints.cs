using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Swap.Htmx.Events;

namespace Swap.Htmx.Dev;

public static class SwapDevEndpoints
{
    /// <summary>
    /// Maps development-only endpoints for inspecting Swap event chains.
    /// - GET /_swap/dev/events (HTML view)
    /// - GET /_swap/dev/events.json (JSON of chains)
    /// </summary>
    public static IEndpointRouteBuilder MapSwapHtmxDevEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var app = endpoints.CreateApplicationBuilder();
        var env = app.ApplicationServices.GetRequiredService<Microsoft.Extensions.Hosting.IHostEnvironment>();
        if (!env.IsDevelopment())
        {
            // No-op outside Development
            return endpoints;
        }

        endpoints.MapGet("/_swap/dev/events.json", async context =>
        {
            var options = context.RequestServices.GetRequiredService<SwapEventBusOptions>();
            var chains = options.GetChainsSnapshot();
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(chains);
        }).WithDisplayName("Swap Events (JSON)");

        endpoints.MapGet("/_swap/dev/events", async context =>
        {
            var options = context.RequestServices.GetRequiredService<SwapEventBusOptions>();
            var chains = options.GetChainsSnapshot();
            var html = RenderHtml(chains);
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync(html);
        }).WithDisplayName("Swap Events (HTML)");

        return endpoints;
    }

    private static string RenderHtml(IReadOnlyDictionary<string, IReadOnlyCollection<string>> chains)
    {
        var enc = HtmlEncoder.Default;
        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html>");
        sb.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\"/><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"/>");
        sb.AppendLine("<title>Swap Event Chains (dev)</title>");
        sb.AppendLine("<style>body{font-family:system-ui,-apple-system,Segoe UI,Roboto,Ubuntu,Cantarell,Noto Sans,sans-serif;line-height:1.4;margin:24px;}h1{font-size:20px;margin:0 0 12px}table{border-collapse:collapse;width:100%;max-width:1100px}th,td{border:1px solid #eee;padding:8px 10px;text-align:left}th{background:#fafafa}code{background:#f6f8fa;padding:2px 4px;border-radius:4px} .pill{display:inline-block;background:#eef6ff;color:#0b5cab;border:1px solid #cfe3ff;padding:2px 6px;border-radius:999px;margin:2px 4px 2px 0;font-size:12px} .muted{color:#666} .grid{display:grid;grid-template-columns:1fr 1fr;gap:16px} .card{border:1px solid #eee;border-radius:8px;padding:12px}</style>");
        sb.AppendLine("</head><body>");
        sb.AppendLine("<h1>Swap Event Chains <span class=\"muted\">(development)</span></h1>");
        sb.AppendLine("<p class=\"muted\">This page reflects the currently configured event chains. It updates automatically when you change chains and refresh.</p>");

        // Summary
        var edgeCount = chains.Sum(kv => kv.Value.Count);
        sb.AppendLine($"<div class=\"grid\"><div class=\"card\"><div><strong>Triggers</strong></div><div>{chains.Count}</div></div>" +
                      $"<div class=\"card\"><div><strong>Edges</strong></div><div>{edgeCount}</div></div></div>");

        // Table view
        sb.AppendLine("<h2>Chains</h2>");
        sb.AppendLine("<table><thead><tr><th>Trigger</th><th>Chained events</th></tr></thead><tbody>");
        foreach (var (trigger, nexts) in chains.OrderBy(k => k.Key))
        {
            sb.Append("<tr><td><code>").Append(enc.Encode(trigger)).Append("</code></td><td>");
            if (nexts.Count == 0)
            {
                sb.Append("<span class=\"muted\">(none)</span>");
            }
            else
            {
                foreach (var n in nexts.OrderBy(s => s))
                {
                    sb.Append("<span class=\"pill\"><code>").Append(enc.Encode(n)).Append("</code></span>");
                }
            }
            sb.Append("</td></tr>");
        }
        sb.AppendLine("</tbody></table>");

        sb.AppendLine("<p class=\"muted\">JSON: <a href=\"/_swap/dev/events.json\">/_swap/dev/events.json</a></p>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }
}
