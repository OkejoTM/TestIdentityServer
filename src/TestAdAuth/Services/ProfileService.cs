using System.Security.Claims;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Identity;
using TestAdAuth.Models;

namespace TestAdAuth.Services;

public class ProfileService(
    UserManager<ApplicationUser> userManager,
    IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory)
    : IProfileService
{
    public async Task GetProfileDataAsync(ProfileDataRequestContext context)
    {
        var sub = context.Subject?.GetSubjectId();
        if (sub == null) return;

        var user = await userManager.FindByIdAsync(sub);
        if (user == null) return;

        var principal = await claimsFactory.CreateAsync(user);
        var claims = principal.Claims.ToList();

        // Добавляем роли
        var roles = await userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        // Добавляем дополнительные claims
        if (!string.IsNullOrEmpty(user.FirstName))
            claims.Add(new Claim("given_name", user.FirstName));
        
        if (!string.IsNullOrEmpty(user.LastName))
            claims.Add(new Claim("family_name", user.LastName));

        context.IssuedClaims = claims;
    }

    public async Task IsActiveAsync(IsActiveContext context)
    {
        var sub = context.Subject?.GetSubjectId();
        if (sub == null)
        {
            context.IsActive = false;
            return;
        }

        var user = await userManager.FindByIdAsync(sub);
        context.IsActive = user != null;
    }
}
