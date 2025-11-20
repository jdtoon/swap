using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;
using Swap.Htmx.ServerSentEvents;
using SwapChat.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Swap.Htmx services
builder.Services.AddSwapHtmx();
builder.Services.AddSseEventBridge();

// Register our custom backplane for distributed demo
builder.Services.AddSingleton<ISseBackplane, FileSseBackplane>();

var app = builder.Build();

app.UseStaticFiles();
app.UseSwapHtmx();

// Map the SSE endpoint
app.MapGet("/swap/sse", (ISseConnectionRegistry registry, HttpContext context) => 
{
    var room = context.Request.Query["room"].ToString();
    
    return SwapResults.Sse(registry, options => {
        options.HeartbeatInterval = TimeSpan.FromSeconds(10);
        if (!string.IsNullOrEmpty(room))
        {
            options.AutoSubscribeRooms = new[] { room };
        }
    });
});

app.MapGet("/", () => Results.Redirect("/index.html"));

// Chat endpoints
app.MapPost("/chat/send", async (
    [FromForm] string message, 
    [FromForm] string username, 
    [FromForm] string room,
    ISseConnectionRegistry registry,
    ILogger<Program> logger) =>
{
    try 
    {
        var html = $@"<div class='message'><strong>{username}:</strong> {message}</div>";
        
        // This will go to the backplane -> file -> all instances -> all clients in room
        await registry.BroadcastToRoomsAsync("chat-message", html, new[] { room });
        
        return Results.Ok();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error sending message");
        return Results.Problem(ex.Message);
    }
});

app.MapGet("/chat/join", (string room, string username) => 
{
    // Return the chat UI
    return Results.Content($@"
        <div hx-ext='sse' sse-connect='/swap/sse?room={room}'>
            <div class='chat-header'>
                <h3>Room: {room}</h3>
                <div id='connection-status' class='status-connecting' title='Connection Status'></div>
            </div>
            <div id='chat-window' sse-swap='chat-message' hx-swap='beforeend'>
                <div class='system-message'>Joined room: {room} as {username}</div>
            </div>
            <form hx-post='/chat/send' hx-swap='none'>
                <input type='hidden' name='username' value='{username}' />
                <input type='hidden' name='room' value='{room}' />
                <input type='text' name='message' placeholder='Type a message...' required autofocus />
                <button type='submit'>Send</button>
            </form>
        </div>
        <script>
            document.body.addEventListener('htmx:sseOpen', function(evt) {{
                var status = document.getElementById('connection-status');
                if(status) status.className = 'status-connected';
            }});
            document.body.addEventListener('htmx:sseError', function(evt) {{
                var status = document.getElementById('connection-status');
                if(status) status.className = 'status-disconnected';
            }});
        </script>
    ", "text/html");
});

app.Run();
