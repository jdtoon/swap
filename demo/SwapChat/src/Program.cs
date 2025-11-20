using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Swap.Htmx;
using Swap.Htmx.Realtime;
using SwapChat.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => {
        options.LoginPath = "/Login";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    });
builder.Services.AddAuthorization();

// Add Razor services (required for Swap.Htmx view rendering)
builder.Services.AddRazorPages();

// Add Swap.Htmx services
builder.Services.AddSwapHtmx();
builder.Services.AddSseEventBridge();

// Register our custom backplane for distributed demo
builder.Services.AddSingleton<ISseBackplane, FileSseBackplane>();

var app = builder.Build();

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();
app.UseSwapHtmx();

app.MapRazorPages();

// Map the SSE endpoint
app.MapGet("/swap/sse", (ISseConnectionRegistry registry, HttpContext context) => 
{
    var room = context.Request.Query["room"].ToString();
    
    return SwapResults.Sse(registry, options => {
        options.HeartbeatInterval = TimeSpan.FromSeconds(10);
        
        // Security: Validate room access
        options.CanJoinRoom = (connection, roomName) => 
        {
            // Anyone can join general rooms
            if (!roomName.Equals("admin", StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(true);
                
            // Only Admins can join the admin room
            return Task.FromResult(connection.User.IsInRole("Admin"));
        };

        if (!string.IsNullOrEmpty(room))
        {
            options.AutoSubscribeRooms = new[] { room };
        }
    });
}).RequireAuthorization();

app.MapGet("/", () => Results.Redirect("/Chat"));

app.MapGet("/logout", async (HttpContext context) => 
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/Login");
});

// Chat endpoints
app.MapPost("/chat/send", async (
    [FromForm] string message, 
    [FromForm] string room,
    ClaimsPrincipal user,
    ISseConnectionRegistry registry,
    ILogger<Program> logger) =>
{
    try 
    {
        var username = user.Identity.Name;
        var html = $@"<div class='message'><strong>{username}:</strong> {message}</div>";
        
        await registry.BroadcastToRoomsAsync("chat-message", html, new[] { room });
        
        return Results.Ok();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error sending message");
        return Results.Problem(ex.Message);
    }
}).RequireAuthorization().DisableAntiforgery();

app.MapPost("/chat/private", async (
    [FromForm] string targetUser,
    [FromForm] string message,
    ClaimsPrincipal user,
    ISseConnectionRegistry registry) =>
{
    var sender = user.Identity.Name;
    var html = $@"<div class='message private'><strong>[Private from {sender}]:</strong> {message}</div>";
    
    // Send to specific user
    await registry.BroadcastToUserAsync("chat-message", html, targetUser);
    
    return Results.Ok();
}).RequireAuthorization().DisableAntiforgery();

app.MapPost("/chat/broadcast", async (
    [FromForm] string message,
    ClaimsPrincipal user,
    ISseConnectionRegistry registry) =>
{
    if (!user.IsInRole("Admin")) return Results.Forbid();

    var sender = user.Identity.Name;
    var html = $@"<div class='message admin'><strong>[ADMIN BROADCAST]:</strong> {message}</div>";
    
    // Send to EVERYONE
    await registry.BroadcastAsync("chat-message", html);
    
    return Results.Ok();
}).RequireAuthorization().DisableAntiforgery();


app.Run();
