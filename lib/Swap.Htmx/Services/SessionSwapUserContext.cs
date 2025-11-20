using Microsoft.AspNetCore.Http;

namespace Swap.Htmx.Services;

/// <summary>
/// Default implementation that uses ASP.NET Core Session.
/// </summary>
public class SessionSwapUserContext : ISwapUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SessionSwapUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetSessionId()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) throw new InvalidOperationException("HttpContext is not available.");

        // Check if Session is available
        try 
        {
            var session = context.Session;
            const string InitKey = "_swap_session_initialized";
            
            // Ensure session cookie is sent by writing a value if not already present
            if (!session.Keys.Contains(InitKey))
            {
                session.SetString(InitKey, DateTime.UtcNow.ToString("O"));
            }
            
            return session.Id;
        }
        catch (InvalidOperationException)
        {
            // Session middleware might not be configured
            throw new InvalidOperationException("Session is not configured. Please register ISwapUserContext with a custom implementation or enable Session middleware.");
        }
    }
}
