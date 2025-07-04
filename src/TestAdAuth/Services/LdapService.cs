using Microsoft.Extensions.Options;
using Novell.Directory.Ldap;

namespace TestAdAuth.Services;

public class LdapSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 389;
    public string BaseDn { get; set; } = string.Empty;
    public string BindDn { get; set; } = string.Empty;
    public string BindPassword { get; set; } = string.Empty;
    public bool UseSsl { get; set; } = false;
}

public class LdapService : ILdapService
{
    private readonly LdapSettings _settings;
    private readonly ILogger<LdapService> _logger;

    public LdapService(IOptions<LdapSettings> settings, ILogger<LdapService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<LdapUser?> AuthenticateAsync(string username, string password)
    {
        try
        {
            using var connection = new LdapConnection();
            
            if (_settings.UseSsl)
            {
                connection.SecureSocketLayer = true;
            }

            await Task.Run(() => connection.Connect(_settings.Host, _settings.Port));
            
            // Для OpenLDAP используем uid вместо sAMAccountName
            var searchFilter = $"(uid={username})";
            var user = await SearchUserAsync(connection, searchFilter);
            
            if (user == null)
            {
                _logger.LogWarning("User {Username} not found in LDAP", username);
                return null;
            }

            // Пробуем аутентифицироваться с паролем пользователя
            try
            {
                await Task.Run(() => connection.Bind(user.DistinguishedName, password));
                user.Groups = await GetUserGroupsAsync(username);
                return user;
            }
            catch (LdapException ex)
            {
                _logger.LogWarning(ex, "Failed to authenticate user {Username}", username);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LDAP connection error");
            return null;
        }
    }

    public async Task<List<string>> GetUserGroupsAsync(string username)
    {
        var groups = new List<string>();

        try
        {
            using var connection = new LdapConnection();
            
            if (_settings.UseSsl)
            {
                connection.SecureSocketLayer = true;
            }

            await Task.Run(() => connection.Connect(_settings.Host, _settings.Port));
            await Task.Run(() => connection.Bind(_settings.BindDn, _settings.BindPassword));

            // Для OpenLDAP ищем группы через memberUid
            var searchFilter = $"(&(objectClass=posixGroup)(memberUid={username}))";
            var searchBase = _settings.BaseDn;

            var search = await Task.Run(() => connection.Search(
                searchBase,
                LdapConnection.ScopeSub,
                searchFilter,
                new[] { "cn" },
                false
            ));

            while (search.HasMore())
            {
                var entry = search.Next();
                var cn = entry.GetAttribute("cn")?.StringValue;
                if (!string.IsNullOrEmpty(cn))
                {
                    groups.Add(cn);
                }
            }

            // Также проверяем группы через memberOf (если используется overlay)
            var userFilter = $"(uid={username})";
            var userSearch = await Task.Run(() => connection.Search(
                searchBase,
                LdapConnection.ScopeSub,
                userFilter,
                new[] { "memberOf" },
                false
            ));

            if (userSearch.HasMore())
            {
                var entry = userSearch.Next();
                var memberOf = entry.GetAttribute("memberOf");
                
                if (memberOf != null)
                {
                    foreach (var group in memberOf.StringValueArray)
                    {
                        var cn = ExtractCn(group);
                        if (!string.IsNullOrEmpty(cn) && !groups.Contains(cn))
                        {
                            groups.Add(cn);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user groups for {Username}", username);
        }

        return groups;
    }

    private async Task<LdapUser?> SearchUserAsync(LdapConnection connection, string searchFilter)
    {
        await Task.Run(() => connection.Bind(_settings.BindDn, _settings.BindPassword));

        var searchBase = _settings.BaseDn;
        // Для OpenLDAP используем другие атрибуты
        var attributes = new[] { "uid", "mail", "cn", "sn", "givenName", "distinguishedName", "entryDN" };

        var search = await Task.Run(() => connection.Search(
            searchBase,
            LdapConnection.ScopeSub,
            searchFilter,
            attributes,
            false
        ));

        if (await Task.Run(() => search.HasMore()))
        {
            var entry = search.Next();
            
            // Получаем DN (в OpenLDAP это может быть entryDN или DN самой записи)
            var dn = entry.Dn;
            
            // Пытаемся получить полное имя из cn или составить из givenName + sn
            var cn = entry.GetAttribute("cn")?.StringValue ?? string.Empty;
            var givenName = entry.GetAttribute("givenName")?.StringValue ?? string.Empty;
            var sn = entry.GetAttribute("sn")?.StringValue ?? string.Empty;
            
            // Если нет givenName/sn, пытаемся разделить cn
            if (string.IsNullOrEmpty(givenName) && string.IsNullOrEmpty(sn) && !string.IsNullOrEmpty(cn))
            {
                var parts = cn.Split(' ', 2);
                if (parts.Length > 0) givenName = parts[0];
                if (parts.Length > 1) sn = parts[1];
            }
            
            return new LdapUser
            {
                Username = entry.GetAttribute("uid")?.StringValue ?? string.Empty,
                Email = entry.GetAttribute("mail")?.StringValue ?? string.Empty,
                FirstName = givenName,
                LastName = sn,
                DistinguishedName = dn
            };
        }

        return null;
    }

    private static string ExtractCn(string dn)
    {
        var parts = dn.Split(',');
        foreach (var part in parts)
        {
            if (part.Trim().StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
            {
                return part.Trim().Substring(3);
            }
        }
        return string.Empty;
    }
}
