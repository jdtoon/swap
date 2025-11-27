using Microsoft.AspNetCore.Mvc;
using Swap.Htmx.Realtime;

namespace Swap.Htmx.Dev.Controllers;

/// <summary>
/// Example controller demonstrating how to pair Swap's SSE primitives with
/// the optional polling helpers in <see cref="Swap.Htmx.Realtime.SseFallbackExtensions"/>.
/// These endpoints are intended for development and documentation only.
/// </summary>
internal class SseFallbackExampleController : SwapController
{
    private readonly ISseFallbackService _fallbackService;
    private readonly ISseConnectionRegistry _connectionRegistry;
    private static readonly Dictionary<string, List<string>> _chatRooms = new();

    public SseFallbackExampleController(
        ISseFallbackService fallbackService,
        ISseConnectionRegistry connectionRegistry)
    {
        _fallbackService = fallbackService;
        _connectionRegistry = connectionRegistry;
    }

    /// <summary>
    /// Basic SSE endpoint with an accompanying polling endpoint.
    /// GET /sse-fallback-example/time      - SSE stream
    /// GET /sse-fallback-example/time/poll - polling endpoint returning HTML snapshots.
    /// </summary>
    [HttpGet("time")]
    public async Task<IActionResult> TimeStream()
    {
        // Check if this should use polling fallback
        if (_fallbackService.ShouldUsePolling(HttpContext))
        {
            return await this.PollingFallback(async lastEventId =>
            {
                var now = DateTime.Now;
                return $"<div id=\"current-time\">{now:HH:mm:ss}</div>";
            });
        }

        // Return SSE stream
        return new EnhancedServerSentEventsResult(async (connectionBuilder, cancellationToken) =>
        {
            var connection = connectionBuilder.Connection;
            while (!cancellationToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                var html = $"<div id=\"current-time\">{now:HH:mm:ss}</div>";
                await connection.SendEventAsync("time-update", html);
                await Task.Delay(1000, cancellationToken);
            }
        });
    }

    /// <summary>
    /// Cached polling endpoint for better performance.
    /// Updates are cached for 5 seconds to reduce server load.
    /// </summary>
    [HttpGet("time/poll")]
    public async Task<IActionResult> TimeStreamPoll()
    {
        return await this.CachedPollingFallback(
            cacheKey: "time-updates",
            getContentFunc: async () =>
            {
                var now = DateTime.Now;
                return $"<div id=\"current-time\">{now:HH:mm:ss}</div>";
            },
            cacheDuration: TimeSpan.FromSeconds(5)
        );
    }

    /// <summary>
    /// Chat room example with SSE and polling support.
    /// Shows how to handle more complex scenarios with user state.
    /// </summary>
    [HttpGet("chat/{room}")]
    public async Task<IActionResult> ChatRoom(string room)
    {
        // Initialize room if it doesn't exist
        if (!_chatRooms.ContainsKey(room))
        {
            _chatRooms[room] = new List<string>();
        }

        if (_fallbackService.ShouldUsePolling(HttpContext))
        {
            return await this.JsonPollingFallback(async lastEventId =>
            {
                var messages = _chatRooms[room];

                // If we have a last event ID, only return newer messages
                if (long.TryParse(lastEventId, out var lastTimestamp))
                {
                    // In a real app, you'd filter by timestamp
                    // For demo, just return the last few messages
                    return new { messages = messages.TakeLast(5), timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };
                }

                return new { messages = messages.TakeLast(10), timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };
            });
        }

        // Return SSE stream
        return new EnhancedServerSentEventsResult(async (connectionBuilder, cancellationToken) =>
        {
            var connection = connectionBuilder.Connection;
            // Send initial messages
            var initialMessages = _chatRooms[room].TakeLast(10);
            foreach (var message in initialMessages)
            {
                var messageHtml = $"<div class=\"message\">{message}</div>";
                await connection.SendEventAsync("message", messageHtml);
            }

            // Join the room for future updates
            connectionBuilder.WithRooms(room);

            // Keep connection alive
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(30000, cancellationToken); // 30 second heartbeat
                await connection.SendEventAsync("heartbeat", "");
            }
        });
    }

    /// <summary>
    /// Post a message to a chat room.
    /// This will trigger updates to both SSE and polling clients.
    /// </summary>
    [HttpPost("chat/{room}/message")]
    public async Task<IActionResult> PostMessage(string room, [FromForm] string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return BadRequest();
        }

        // Add message to room
        if (!_chatRooms.ContainsKey(room))
        {
            _chatRooms[room] = new List<string>();
        }

        var timestamp = DateTime.Now.ToString("HH:mm");
        var formattedMessage = $"[{timestamp}] {message}";
        _chatRooms[room].Add(formattedMessage);

        // Broadcast to SSE clients
        var messageHtml = $"<div class=\"message\">{formattedMessage}</div>";
        await _connectionRegistry.BroadcastToRoomsAsync("message", messageHtml, new[] { room });

        // Return success response for the posting client
        return PartialView("_MessagePosted", formattedMessage);
    }

    /// <summary>
    /// Example view that previously demonstrated automatic client-side
    /// fallback using a JavaScript extension. The server-side pieces remain
    /// valid as a reference for wiring SSE and polling together.
    /// </summary>
    [HttpGet("auto-fallback")]
    public IActionResult AutoFallbackExample()
    {
        return View("AutoFallback");
    }

    /// <summary>
    /// Data endpoint that supports both SSE and polling via the same URL.
    /// Uses content negotiation to determine the response format.
    /// </summary>
    [HttpGet("notifications")]
    public async Task<IActionResult> Notifications()
    {
        var accepts = Request.Headers["Accept"].ToString();

        if (accepts.Contains("text/event-stream") && !_fallbackService.ShouldUsePolling(HttpContext))
        {
            // SSE stream
            return new EnhancedServerSentEventsResult(async (connectionBuilder, cancellationToken) =>
            {
                var connection = connectionBuilder.Connection;
                // Simulate notifications
                var notifications = new[]
                {
                    "Welcome to the notification system!",
                    "You have 3 new messages",
                    "System maintenance in 1 hour",
                    "New feature available: Dark mode"
                };

                foreach (var notification in notifications)
                {
                    var html = $"<div class=\"notification\">{notification}</div>";
                    await connection.SendEventAsync("notification", html);
                    await Task.Delay(2000, cancellationToken);
                }

                // Keep connection alive for future notifications
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(30000, cancellationToken);
                }
            });
        }
        else
        {
            // Polling fallback
            return await this.JsonPollingFallback(async lastEventId =>
            {
                // In a real app, you'd query a database for new notifications
                // For demo, return a sample notification
                var hasNewNotification = Random.Shared.Next(0, 3) == 0;

                if (hasNewNotification)
                {
                    return new
                    {
                        notification = $"New update at {DateTime.Now:HH:mm:ss}",
                        timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    };
                }

                return null; // No new notifications
            });
        }
    }
}