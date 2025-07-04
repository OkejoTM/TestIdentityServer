using Microsoft.AspNetCore.Identity;
using TestAdAuth.Models;

namespace TestAdAuth.Services;

public class UserSyncService(
    UserManager<ApplicationUser> userManager,
    ILogger<UserSyncService> logger)
    : IUserSyncService
{
    private readonly Dictionary<string, string> _groupToRoleMapping = new()
    {
        { "employees", "Employee" },
        { "office-managers", "OfficeManager" },
        // Добавьте другие маппинги по необходимости
    };

    // Маппинг групп AD на роли
    // Добавьте другие маппинги по необходимости

    public async Task<ApplicationUser?> SyncUserFromLdapAsync(LdapUser ldapUser)
    {
        var user = await userManager.FindByNameAsync(ldapUser.Username);
        
        if (user == null)
        {
            // Создание нового пользователя
            user = new ApplicationUser
            {
                UserName = ldapUser.Username,
                Email = ldapUser.Email,
                FirstName = ldapUser.FirstName,
                LastName = ldapUser.LastName,
                LdapDn = ldapUser.DistinguishedName,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true // LDAP пользователи считаются подтвержденными
            };

            var result = await userManager.CreateAsync(user);
            
            if (!result.Succeeded)
            {
                logger.LogError("Failed to create user {Username}: {Errors}", 
                    ldapUser.Username, 
                    string.Join(", ", result.Errors.Select(e => e.Description)));
                return null;
            }
        }
        else
        {
            // Обновление существующего пользователя
            user.Email = ldapUser.Email;
            user.FirstName = ldapUser.FirstName;
            user.LastName = ldapUser.LastName;
            user.LdapDn = ldapUser.DistinguishedName;

            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                logger.LogError("Failed to update user {Username}: {Errors}", 
                    ldapUser.Username, 
                    string.Join(", ", updateResult.Errors.Select(e => e.Description)));
            }
        }

        // Синхронизация ролей
        await SyncUserRolesAsync(user, ldapUser.Groups);

        return user;
    }

    private async Task SyncUserRolesAsync(ApplicationUser user, List<string> ldapGroups)
    {
        // Получение текущих ролей пользователя
        var currentRoles = await userManager.GetRolesAsync(user);

        // Определение новых ролей на основе групп LDAP
        var newRoles = new List<string>();
        foreach (var group in ldapGroups)
        {
            if (_groupToRoleMapping.TryGetValue(group, out var value))
            {
                newRoles.Add(value);
            }
        }

        // Если групп нет, назначаем роль Employee по умолчанию
        if (!newRoles.Any())
        {
            newRoles.Add("Employee");
        }

        // Удаление старых ролей
        var rolesToRemove = currentRoles.Except(newRoles).ToList();
        if (rolesToRemove.Any())
        {
            var removeResult = await userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
            {
                logger.LogError("Failed to remove roles for user {Username}: {Errors}", 
                    user.UserName, 
                    string.Join(", ", removeResult.Errors.Select(e => e.Description)));
            }
        }

        // Добавление новых ролей
        var rolesToAdd = newRoles.Except(currentRoles).ToList();
        if (rolesToAdd.Any())
        {
            var addResult = await userManager.AddToRolesAsync(user, rolesToAdd);
            if (!addResult.Succeeded)
            {
                logger.LogError("Failed to add roles for user {Username}: {Errors}", 
                    user.UserName, 
                    string.Join(", ", addResult.Errors.Select(e => e.Description)));
            }
        }
    }
}
