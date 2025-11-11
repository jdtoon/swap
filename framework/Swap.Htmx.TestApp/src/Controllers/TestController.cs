using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;

namespace Swap.Htmx.TestApp.Controllers;

/// <summary>
/// Controller for testing Swap.Htmx framework features
/// </summary>
public class TestController : SwapController
{
    // Toast Tests
    
    [HttpGet("/test")]
    public IActionResult Index()
    {
        return SwapView();
    }
    
    [HttpPost("/test/toast/success")]
    public IActionResult ToastSuccess()
    {
        Response.ShowSuccessToast("Success! This is a success toast notification.");
        return SwapView("ToastResult");
    }
    
    [HttpPost("/test/toast/error")]
    public IActionResult ToastError()
    {
        Response.ShowErrorToast("Error! Something went wrong.");
        return SwapView("ToastResult");
    }
    
    [HttpPost("/test/toast/warning")]
    public IActionResult ToastWarning()
    {
        Response.ShowWarningToast("Warning! Please review this action.");
        return SwapView("ToastResult");
    }
    
    [HttpPost("/test/toast/info")]
    public IActionResult ToastInfo()
    {
        Response.ShowInfoToast("Info: Just so you know...");
        return SwapView("ToastResult");
    }
    
    // OOB Swap Tests
    
    [HttpPost("/test/oob/single")]
    public IActionResult OobSingle()
    {
        // Update secondary panel out-of-band
        ViewData["OobSecondary"] = RenderOobPanel("secondary-panel", "Secondary panel updated via OOB!");
        return SwapView("OobResult", "Primary content updated");
    }
    
    [HttpPost("/test/oob/multiple")]
    public IActionResult OobMultiple()
    {
        // Update multiple panels out-of-band
        ViewData["OobPanel1"] = RenderOobPanel("panel-1", $"Panel 1 updated at {DateTime.Now:HH:mm:ss}");
        ViewData["OobPanel2"] = RenderOobPanel("panel-2", $"Panel 2 updated at {DateTime.Now:HH:mm:ss}");
        ViewData["OobPanel3"] = RenderOobPanel("panel-3", $"Panel 3 updated at {DateTime.Now:HH:mm:ss}");
        
        return SwapView("OobResult", "All panels updated!");
    }
    
    [HttpPost("/test/oob/counter")]
    public IActionResult OobCounter()
    {
        // Simulate incrementing a counter
        var currentCount = int.Parse(Request.Form["count"].ToString() ?? "0");
        var newCount = currentCount + 1;
        
        ViewData["OobCounter"] = RenderOobPanel("counter-display", $"Count: {newCount}", "innerHTML");
        ViewData["CountValue"] = newCount;
        
        return SwapView("OobResult", $"Counter incremented to {newCount}");
    }
    
    // Combined Tests
    
    [HttpPost("/test/combined")]
    public IActionResult Combined()
    {
        // Test toast + OOB together
        Response.ShowSuccessToast("Both toast and OOB swap working!");
        ViewData["OobStatus"] = RenderOobPanel("status-panel", $"Last updated: {DateTime.Now:HH:mm:ss}");
        
        return SwapView("OobResult", "Combined update complete");
    }
    
    // Helper Methods
    
    private string RenderOobPanel(string targetId, string content, string strategy = "true")
    {
        return $@"<div id=""{targetId}"" data-test-id=""{targetId}"" hx-swap-oob=""{strategy}"" class=""box has-background-info-light"">
            <p class=""has-text-centered"">{content}</p>
        </div>";
    }

    // Server-Sent Events Tests

    [HttpGet("/test/sse")]
    public IActionResult SseDemo()
    {
        return SwapView();
    }

    [HttpGet("/test/sse/start")]
    public IActionResult SseStart()
    {
        return SwapView();
    }

    [HttpGet("/test/sse/stream")]
    public IActionResult SseStream()
    {
        return ServerSentEvents(async (stream, ct) =>
        {
            var notifications = new[]
            {
                "System update completed",
                "New message from Admin",
                "Your report is ready",
                "Scheduled task finished",
                "Database backup completed"
            };

            for (int i = 0; i < notifications.Length; i++)
            {
                if (ct.IsCancellationRequested) break;

                var html = await this.RenderPartialToStringAsync("_SseNotification", (i, notifications[i]));
                await stream.SendEventAsync("notification", html);
                await stream.SendKeepAliveAsync();
                await Task.Delay(1000, ct);
            }

            var finalHtml = await this.RenderPartialToStringAsync("_SseComplete", (object?)null);
            await stream.SendEventAsync("notification", finalHtml);
            
            // Send close event to tell HTMX to close the connection gracefully
            await stream.SendEventAsync("close", "done");
        });
    }
}
