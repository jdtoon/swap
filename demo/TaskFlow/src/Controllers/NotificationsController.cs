using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;
using Swap.Htmx.Extensions;
using Swap.Htmx.ServerSentEvents;
using TaskFlow.Services;
using TaskFlow.Views;

namespace TaskFlow.Controllers;

/// <summary>
/// Demonstrates SSE notification stream and real-time updates
/// </summary>
public class NotificationsController : SwapController
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    // ================================================================================
    // SSE ENDPOINT - Real-time notification stream
    // ================================================================================

    [HttpGet("/notifications/stream")]
    public IActionResult NotificationStream()
    {
        return ServerSentEvents(async (stream, cancel) =>
        {
            // Demonstrates SSE for real-time notifications
            // In production, subscribe to user-specific notification channel
            
            await stream.SendEventAsync("heartbeat", "connected");

            // Push new notifications as they arrive
            // Example: new notification event
            var userId = "demo-user"; // In real app, get from auth
            var unreadCount = _notificationService.GetUnreadCount(userId);
            
            await stream.SendEventAsync("notification-count", unreadCount.ToString());
            
            // Keep connection alive
            while (!cancel.IsCancellationRequested)
            {
                await Task.Delay(30000, cancel);
                if (!cancel.IsCancellationRequested)
                {
                    await stream.SendKeepAliveAsync();
                }
            }
        });
    }

    [HttpGet("/notifications/list")]
    public IActionResult Index()
    {
        var userId = "demo-user"; // In real app, get from auth
        var notifications = _notificationService.GetForUser(userId);
        return SwapView(NotificationViews.Index, notifications);
    }

    [HttpGet("/notifications/bell")]
    public IActionResult GetBell()
    {
        var userId = "demo-user"; // In real app, get from auth
        var unreadCount = _notificationService.GetUnreadCount(userId);
        return PartialView(NotificationViews.Bell, unreadCount);
    }

    [HttpPost("/notifications/{id}/read")]
    public IActionResult MarkAsRead(int id)
    {
        var notification = _notificationService.Get(id);
        if (notification == null)
        {
            return SwapResponse()
                .WithToast("Notification not found", ToastType.Error)
                .Build();
        }

        _notificationService.MarkAsRead(id);

        var userId = "demo-user"; // In real app, get from auth
        
        return SwapResponse()
            .AlsoUpdate(NotificationElements.Card(id), NotificationViews.Card, _notificationService.Get(id)!)
            .AlsoUpdate(NotificationElements.Bell, NotificationViews.Bell, _notificationService.GetUnreadCount(userId))
            .Build();
    }

    [HttpPost("/notifications/read-all")]
    public IActionResult MarkAllAsRead()
    {
        var userId = "demo-user"; // In real app, get from auth
        _notificationService.MarkAllAsRead(userId);

        return SwapResponse()
            .AlsoUpdate(NotificationElements.List, NotificationViews.List, _notificationService.GetForUser(userId))
            .AlsoUpdate(NotificationElements.Bell, NotificationViews.Bell, 0)
            .WithToast("All notifications marked as read", ToastType.Success)
            .Build();
    }
}
