using System.Security.Claims;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Validation;
using Microsoft.AspNetCore.Identity;
using TestAdAuth.Models;

namespace TestAdAuth.Services;

public class ResourceOwnerPasswordValidator : IResourceOwnerPasswordValidator
{
    private readonly ILdapService _ldapService;
    private readonly IUserSyncService _userSyncService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ResourceOwnerPasswordValidator> _logger;

    public ResourceOwnerPasswordValidator(
        ILdapService ldapService,
        IUserSyncService userSyncService,
        UserManager<ApplicationUser> userManager,
        ILogger<ResourceOwnerPasswordValidator> logger)
    {
        _ldapService = ldapService;
        _userSyncService = userSyncService;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
    {
        try
        {
            // Аутентификация через LDAP
            var ldapUser = await _ldapService.AuthenticateAsync(context.UserName, context.Password);
            
            if (ldapUser == null)
            {
                _logger.LogWarning("Invalid credentials for user {Username}", context.UserName);
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "Invalid credentials");
                return;
            }

            // Синхронизация с локальной БД
            var user = await _userSyncService.SyncUserFromLdapAsync(ldapUser);
            
            if (user == null)
            {
                _logger.LogError("Failed to sync user {Username} from LDAP", context.UserName);
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "User sync failed");
                return;
            }

            // Получение ролей пользователя
            var roles = await _userManager.GetRolesAsync(user);

            // Создание claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim("given_name", user.FirstName ?? string.Empty),
                new Claim("family_name", user.LastName ?? string.Empty)
            };

            // Добавление ролей в claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            context.Result = new GrantValidationResult(
                subject: user.Id.ToString(),
                authenticationMethod: "ldap",
                claims: claims
            );

            // Обновление времени последнего входа
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password validation");
            context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "Authentication error");
        }
    }
}