using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Swap.Htmx.Stories;

/// <summary>
/// Middleware that serves the SwapStories component playground dashboard.
/// Only active in Development environment.
/// </summary>
internal class SwapStoriesMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _env;
    private readonly SwapStoryRegistry _registry;

    public SwapStoriesMiddleware(RequestDelegate next, IWebHostEnvironment env, SwapStoryRegistry registry)
    {
        _next = next;
        _env = env;
        _registry = registry;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_env.IsDevelopment())
        {
            // Fully disable in production
            await _next(context);
            return;
        }

        var path = context.Request.Path;
        if (!path.StartsWithSegments("/_swap/stories"))
        {
            await _next(context);
            return;
        }

        // Handle Dashboard Request
        if (path.Value == "/_swap/stories" || path.Value == "/_swap/stories/")
        {
            await ServeDashboard(context);
            return;
        }

        await _next(context);
    }

    private async Task ServeDashboard(HttpContext context)
    {
        var stories = _registry.GetStories();
        var selectedStoryId = context.Request.Query["storyId"].ToString();
        var selectedStory = !string.IsNullOrEmpty(selectedStoryId)
            ? stories.Values.SelectMany(x => x).FirstOrDefault(s => s.Id == selectedStoryId)
            : null;

        var html = new StringBuilder();
        html.Append($@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""utf-8"">
    <title>SwapStories</title>
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
    <style>
        :root {{
            --bg-color: #ffffff;
            --sidebar-bg: #f8f9fa;
            --border-color: #e9ecef;
            --text-color: #212529;
            --text-muted: #6c757d;
            --nav-hover: #e9ecef;
            --nav-active: #e7f1ff;
            --nav-active-text: #0d6efd;
            --font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, ""Helvetica Neue"", Arial, sans-serif;
            --radius: 6px;
            --shadow: 0 4px 12px rgba(0,0,0,0.08);
            --header-h: 50px;
        }}
        @media (prefers-color-scheme: dark) {{
            :root {{
                --bg-color: #1a1b1e; /* Deep dark grey */
                --sidebar-bg: #141517; /* Even darker */
                --border-color: #2c2e33;
                --text-color: #e1e3e5;
                --text-muted: #adb5bd;
                --nav-hover: #25262b;
                --nav-active: #1971c2;
                --nav-active-text: #ffffff;
                --shadow: 0 4px 12px rgba(0,0,0,0.3);
            }}
        }}
        body {{ margin: 0; font-family: var(--font-family); color: var(--text-color); background: var(--bg-color); height: 100vh; display: flex; overflow: hidden; }}
        
        /* Sidebar */
        .sidebar {{ width: 280px; background: var(--sidebar-bg); border-right: 1px solid var(--border-color); display: flex; flex-direction: column; }}
        .sidebar-header {{ padding: 0 1rem; height: var(--header-h); display: flex; align-items: center; justify-content: space-between; border-bottom: 1px solid var(--border-color); }}
        .logo {{ font-weight: 700; font-size: 1.1rem; text-decoration: none; color: var(--text-color); display: flex; align-items: center; gap: 8px; }}
        .logo svg {{ width: 20px; height: 20px; color: #0d6efd; }} /* Brand color */
        
        .nav-list {{ flex: 1; overflow-y: auto; padding: 1rem; }}
        .category-title {{ font-size: 0.75rem; text-transform: uppercase; letter-spacing: 0.5px; font-weight: 600; color: var(--text-muted); margin: 1.5rem 0 0.5rem 0.5rem; }}
        .category-title:first-child {{ margin-top: 0; }}
        
        .story-link {{ display: block; padding: 0.4rem 0.6rem; margin-bottom: 2px; text-decoration: none; color: var(--text-color); border-radius: var(--radius); font-size: 0.9rem; transition: background 0.15s; }}
        .story-link:hover {{ background: var(--nav-hover); }}
        .story-link.active {{ background: var(--nav-active); color: var(--nav-active-text); font-weight: 500; }}
        .story-desc {{ font-size: 0.8em; color: var(--text-muted); margin-left: 1rem; display: block; }}

        /* Main Area */
        .main {{ flex: 1; display: flex; flex-direction: column; background: var(--bg-color); }}
        .toolbar {{ height: var(--header-h); border-bottom: 1px solid var(--border-color); display: flex; align-items: center; padding: 0 1rem; gap: 1rem; }}
        .story-info {{ flex: 1; font-weight: 600; }}
        .toolbar-actions {{ display: flex; gap: 0.5rem; }}
        
        .btn-icon {{ background: none; border: 1px solid var(--border-color); color: var(--text-color); border-radius: 4px; padding: 6px; cursor: pointer; display: flex; align-items: center; justify-content: center; }}
        .btn-icon:hover {{ background: var(--nav-hover); }}

        .canvas-wrapper {{ flex: 1; background: #e9ecef; /* Neutral grey canvas bg */ padding: 2rem; display: flex; align-items: flex-start; justify-content: center; overflow: auto; position: relative; }}
        @media (prefers-color-scheme: dark) {{ .canvas-wrapper {{ background: #000000; }} }}
        
        /* The Iframe Container - simulates viewport */
        .iframe-container {{ background: #ffffff; box-shadow: var(--shadow); transition: all 0.2s ease; border-radius: var(--radius); overflow: hidden; }}
        iframe {{ border: none; width: 100%; height: 100%; display: block; background: #ffffff; }} /* White bg inside iframe normally */
        @media (prefers-color-scheme: dark) {{ 
            .iframe-container {{ border: 1px solid #333; }}
            iframe {{ background: #1a1b1e; }} 
        }}

        .empty-state {{ text-align: center; color: var(--text-muted); margin-top: 20%; }}
    </style>
</head>
<body>
    <div class=""sidebar"">
        <div class=""sidebar-header"">
            <a href=""/_swap/stories"" class=""logo"">
                <svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 24 24"" fill=""currentColor""><path d=""M12 2L2 7l10 5 10-5-10-5zm0 9l2.5-1.25L12 8.5l-2.5 1.25L12 11zm0 2.5l-5-2.5-5 2.5 10 5 10-5-5-2.5-5 2.5z""/></svg>
                SwapStories
            </a>
        </div>
        <div class=""nav-list"">
");

        // Group categories: "Components" first, then others alphabetically
        var categories = stories.Keys.OrderBy(k => k == "Components" ? "" : k).ToList();

        foreach (var category in categories)
        {
            html.Append($@"<div class=""category-title"">{HtmlEncode(category)}</div>");
            
            foreach (var story in stories[category].OrderBy(s => s.Title))
            {
                var isActive = story.Id == selectedStoryId ? "active" : "";
                html.Append($@"<a href=""?storyId={story.Id}"" class=""story-link {isActive}"">{HtmlEncode(story.Title)}</a>");
            }
        }

        html.Append(@"
        </div>
    </div>
    <div class=""main"">
");

        if (selectedStory != null)
        {
            var width = selectedStory.Width > 0 ? selectedStory.Width + "px" : "100%";
            var height = selectedStory.Height > 0 ? selectedStory.Height + "px" : "100%";
            var containerStyle = selectedStory.Width > 0 || selectedStory.Height > 0 
                ? $"width: {width}; height: {height};" 
                : "width: 100%; height: 100%; border-radius: 0; box-shadow: none;";

            html.Append($@"
        <div class=""toolbar"">
            <div class=""story-info"">{HtmlEncode(selectedStory.Title)} <span style=""font-weight:400; color:var(--text-muted); font-size:0.9em"">— {HtmlEncode(selectedStory.Description ?? "")}</span></div>
            <div class=""toolbar-actions"">
                <!-- Action Buttons: Reload, Pop out -->
                <button class=""btn-icon"" onclick=""document.getElementById('storyFrame').contentWindow.location.reload();"" title=""Refresh"">
                   <svg width=""16"" height=""16"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M23 4v6h-6M1 20v-6h6"" transform=""scale(0.66) translate(1,1)""/><path d=""M3.51 9a9 9 0 0 1 14.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0 0 20.49 15"" transform=""scale(0.66) translate(1,1)""/></svg>
                </button>
                <a href=""{selectedStory.RouteUrl}"" target=""_blank"" class=""btn-icon"" title=""Open in New Tab"">
                    <svg width=""16"" height=""16"" fill=""none"" stroke=""currentColor"" stroke-width=""2"" stroke-linecap=""round"" stroke-linejoin=""round""><path d=""M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6M15 3h6v6M10 14L21 3"" transform=""scale(0.66) translate(1,1)""/></svg>
                </a>
            </div>
        </div>
        <div class=""canvas-wrapper"">
            <div class=""iframe-container"" style=""{containerStyle}"">
                <iframe id=""storyFrame"" src=""{selectedStory.RouteUrl}""></iframe>
            </div>
        </div>
");
        }
        else
        {
            html.Append(@"
        <div class=""empty-state"">
            <h3>Select a story from the sidebar</h3>
            <p>Stories marked with <code>[SwapStory]</code> will appear here.</p>
        </div>
");
        }

        html.Append(@"
    </div>
</body>
</html>");

        context.Response.ContentType = "text/html";
        await context.Response.WriteAsync(html.ToString());
    }

    private string HtmlEncode(string text) => System.Net.WebUtility.HtmlEncode(text);
}
