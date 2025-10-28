using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Swap.Patterns.Auditable;

/// <summary>
/// Extension methods for configuring auditable patterns in ASP.NET Core applications.
/// </summary>
public static class AuditExtensions
{
    /// <summary>
    /// Creates an audit interceptor that uses the current HTTP context user.
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor to retrieve current user.</param>
    /// <param name="claimType">The claim type to use for user identifier (default: NameIdentifier).</param>
    /// <returns>An audit interceptor configured with the HTTP context user provider.</returns>
    /// <example>
    /// <code>
    /// // In Program.cs
    /// builder.Services.AddHttpContextAccessor();
    /// 
    /// // In your DbContext
    /// private readonly IHttpContextAccessor _httpContextAccessor;
    /// 
    /// public AppDbContext(DbContextOptions options, IHttpContextAccessor httpContextAccessor)
    ///     : base(options)
    /// {
    ///     _httpContextAccessor = httpContextAccessor;
    /// }
    /// 
    /// protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    /// {
    ///     optionsBuilder.AddInterceptors(_httpContextAccessor.CreateAuditInterceptor());
    /// }
    /// </code>
    /// </example>
    public static AuditInterceptor CreateAuditInterceptor(
        this IHttpContextAccessor httpContextAccessor,
        string claimType = ClaimTypes.NameIdentifier)
    {
        return new AuditInterceptor(() =>
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                return user.FindFirst(claimType)?.Value
                    ?? user.FindFirst(ClaimTypes.Name)?.Value
                    ?? user.FindFirst(ClaimTypes.Email)?.Value;
            }
            return null;
        });
    }

    /// <summary>
    /// Creates an audit interceptor with a custom user provider function.
    /// </summary>
    /// <param name="userProvider">Function that returns the current user identifier.</param>
    /// <returns>An audit interceptor configured with the specified user provider.</returns>
    /// <example>
    /// <code>
    /// protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    /// {
    ///     optionsBuilder.AddInterceptors(AuditExtensions.CreateAuditInterceptor(
    ///         () => _currentUserService.GetUserId()
    ///     ));
    /// }
    /// </code>
    /// </example>
    public static AuditInterceptor CreateAuditInterceptor(Func<string?> userProvider)
    {
        return new AuditInterceptor(userProvider);
    }
}
