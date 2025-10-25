using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using habits.Data.Models;
using System.Security.Claims;

public class AppUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<AppUser>
{
    public AppUserClaimsPrincipalFactory(UserManager<AppUser> userManager, IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, optionsAccessor)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        // Add custom claims
        if (!string.IsNullOrEmpty(user.Name))
        {
            identity.AddClaim(new Claim("Name", user.Name));
        }

        if (!string.IsNullOrEmpty(user.Surname))
        {
            identity.AddClaim(new Claim("Surname", user.Surname));
        }

        var roles = await UserManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, role));
        }

        return identity;
    }
}
