using Microsoft.AspNetCore.Identity;
using habits.Data.Models;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;

public class ActiveUserMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache; // Use in-memory caching

    public ActiveUserMiddleware(RequestDelegate next, IMemoryCache cache)
    {
        _next = next;
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context, UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
    {
        if (context.User.Identity!.IsAuthenticated)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrEmpty(userId))
            {
                // Check cache first
                var cacheKey = $"UserIsActive-{userId}";
                if (!_cache.TryGetValue(cacheKey, out bool isActive))
                {
                    // Cache miss - fetch from database
                    var user = await userManager.FindByIdAsync(userId);
                    isActive = user?.IsActive ?? false;

                    // Store in cache for 5 minutes
                    _cache.Set(cacheKey, isActive, TimeSpan.FromMinutes(15));
                }

                if (!isActive)
                {
                    await signInManager.SignOutAsync();
                    context.Response.Redirect("/Identity/Account/Login");
                    return;
                }
            }
        }

        await _next(context);
    }
}