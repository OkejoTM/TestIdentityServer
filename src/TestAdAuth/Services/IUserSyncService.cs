using TestAdAuth.Models;

namespace TestAdAuth.Services;

public interface IUserSyncService
{
    Task<ApplicationUser?> SyncUserFromLdapAsync(LdapUser ldapUser);
}