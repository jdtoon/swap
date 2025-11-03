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
    /// - GET /_swap/dev/events.meta.json (JSON of resolution mode/depth)
    /// - GET /_swap/dev/explain.json?event=xyz (resolve set under current mode/depth)
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

        endpoints.MapGet("/_swap/dev/events.meta.json", async context =>
        {
            var options = context.RequestServices.GetRequiredService<SwapEventBusOptions>();
            var meta = new
            {
                resolutionMode = options.ResolutionMode.ToString(),
                maxTransitiveDepth = options.MaxTransitiveDepth
            };
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(meta);
        }).WithDisplayName("Swap Events Meta (JSON)");

        endpoints.MapGet("/_swap/dev/explain.json", async context =>
        {
            var options = context.RequestServices.GetRequiredService<SwapEventBusOptions>();
            var ev = context.Request.Query["event"].ToString();
            if (string.IsNullOrWhiteSpace(ev))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new { error = "Provide ?event=<name>" });
                return;
            }

            var resolved = ExplainResolve(ev, options);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { @event = ev, resolved = resolved.OrderBy(s => s).ToArray() });
        }).WithDisplayName("Swap Events Explain (JSON)");

        endpoints.MapGet("/_swap/dev/events", async context =>
        {
            var options = context.RequestServices.GetRequiredService<SwapEventBusOptions>();
            var chains = options.GetChainsSnapshot();
            var html = RenderHtml(chains, options);
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync(html);
        }).WithDisplayName("Swap Events (HTML)");

        return endpoints;
    }

    private static string RenderHtml(IReadOnlyDictionary<string, IReadOnlyCollection<string>> chains, SwapEventBusOptions options)
    {
        var enc = HtmlEncoder.Default;
        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html>");
        sb.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\"/><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"/>");
        sb.AppendLine("<title>Swap Event Chains (dev)</title>");
        sb.AppendLine("<style>body{font-family:system-ui,-apple-system,Segoe UI,Roboto,Ubuntu,Cantarell,Noto Sans,sans-serif;line-height:1.4;margin:24px;}h1{font-size:20px;margin:0 0 12px}table{border-collapse:collapse;width:100%;max-width:1100px}th,td{border:1px solid #eee;padding:8px 10px;text-align:left}th{background:#fafafa}code{background:#f6f8fa;padding:2px 4px;border-radius:4px} .pill{display:inline-block;background:#eef6ff;color:#0b5cab;border:1px solid #cfe3ff;padding:2px 6px;border-radius:999px;margin:2px 4px 2px 0;font-size:12px} .muted{color:#666} .grid{display:grid;grid-template-columns:repeat(3,1fr);gap:16px} .card{border:1px solid #eee;border-radius:8px;padding:12px} .section{margin-top:20px} .row{display:flex;gap:8px;align-items:center}</style>");
        sb.AppendLine("<script src=\"https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.min.js\"></script>");
        sb.AppendLine("<script>if(window.mermaid){mermaid.initialize({startOnLoad:true});}</script>");
        sb.AppendLine("</head><body>");
        sb.AppendLine("<h1>Swap Event Chains <span class=\"muted\">(development)</span></h1>");
        sb.AppendLine($"<p class=\"muted\">ResolutionMode: <code>{enc.Encode(options.ResolutionMode.ToString())}</code> · MaxTransitiveDepth: <code>{options.MaxTransitiveDepth}</code></p>");
        sb.AppendLine("<p class=\"muted\">This page reflects the currently configured event chains. It updates automatically when you change chains and refresh.</p>");

        // Summary
        var edgeCount = chains.Sum(kv => kv.Value.Count);
        sb.AppendLine($"<div class=\"grid\"><div class=\"card\"><div><strong>Triggers</strong></div><div>{chains.Count}</div></div>" +
                      $"<div class=\"card\"><div><strong>Edges</strong></div><div>{edgeCount}</div></div>" +
                      $"<div class=\"card\"><div><strong>Endpoints</strong></div><div><a href=\"/_swap/dev/events.json\">events.json</a> · <a href=\"/_swap/dev/events.meta.json\">events.meta.json</a></div></div></div>");

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

        // Mermaid graph
        var mermaid = BuildMermaid(chains);
        sb.AppendLine("<div class=\"section\"><h2>Graph</h2>");
        sb.Append("<pre class=\"mermaid\">\n").Append(enc.Encode(mermaid)).AppendLine("\n</pre></div>");

        // Explain form
        sb.AppendLine("<div class=\"section\"><h2>Explain</h2><div class=\"row\"><input id=\"ev\" placeholder=\"event name e.g. todo.created\" style=\"width:340px;padding:6px;border:1px solid #ccc;border-radius:6px\"/><button id=\"btn\" style=\"padding:6px 10px\">Resolve</button><span class=\"muted\">under current mode/depth</span></div><div id=\"out\" class=\"section\"></div></div>");
        sb.AppendLine("<script>document.getElementById('btn').addEventListener('click',async()=>{const ev=document.getElementById('ev').value; if(!ev) return; const res=await fetch('/_swap/dev/explain.json?event='+encodeURIComponent(ev)); const data=await res.json(); const out=document.getElementById('out'); if(data.error){out.innerHTML='<span style=\\'color:red\\'>'+data.error+'</span>';return;} out.innerHTML='<div><strong>Resolved ('+data.resolved.length+')</strong></div>'+data.resolved.map(e=>'\n<span class=\\'pill\\'><code>'+e+'</code></span>').join('');});</script>");

        sb.AppendLine("<p class=\"muted\">JSON: <a href=\"/_swap/dev/events.json\">/_swap/dev/events.json</a> · Meta: <a href=\"/_swap/dev/events.meta.json\">/_swap/dev/events.meta.json</a> · Explain: <code>/_swap/dev/explain.json?event=...</code></p>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static IEnumerable<string> ExplainResolve(string root, SwapEventBusOptions options)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { root };
        switch (options.ResolutionMode)
        {
            case Swap.Htmx.Events.ChainResolutionMode.OneHop:
                if (options.Chains.TryGetValue(root, out var nexts))
                {
                    foreach (var n in nexts) set.Add(n);
                }
                break;
            case Swap.Htmx.Events.ChainResolutionMode.Bidirectional:
                if (options.Chains.TryGetValue(root, out var nextsB))
                {
                    foreach (var n in nextsB) set.Add(n);
                }
                foreach (var kv in options.Chains)
                {
                    if (kv.Value.Contains(root)) set.Add(kv.Key);
                }
                break;
            case Swap.Htmx.Events.ChainResolutionMode.Transitive:
                int max = Math.Max(1, options.MaxTransitiveDepth);
                var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { root };
                var q = new Queue<(string node,int depth)>();
                q.Enqueue((root,0));
                while (q.Count>0)
                {
                    var (node, depth) = q.Dequeue();
                    if (depth>=max) continue;
                    if (options.Chains.TryGetValue(node, out var nx))
                    {
                        foreach (var n in nx)
                        {
                            if (visited.Add(n)) { set.Add(n); q.Enqueue((n, depth+1)); }
                        }
                    }
                }
                break;
        }
        return set;
    }

    private static string BuildMermaid(IReadOnlyDictionary<string, IReadOnlyCollection<string>> chains)
    {
        var sb = new StringBuilder();
        sb.AppendLine("graph LR");
        foreach (var kv in chains.OrderBy(k=>k.Key))
        {
            foreach (var n in kv.Value.OrderBy(x=>x))
            {
                // Quote in case of dots
                sb.AppendLine($"\"{kv.Key}\" --> \"{n}\"");
            }
        }
        return sb.ToString();
    }
}
