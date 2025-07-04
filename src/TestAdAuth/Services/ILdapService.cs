namespace TestAdAuth.Services;

public interface ILdapService
{
    Task<LdapUser?> AuthenticateAsync(string username, string password);
    Task<List<string>> GetUserGroupsAsync(string username);
}

public class LdapUser
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DistinguishedName { get; set; } = string.Empty;
    public List<string> Groups { get; set; } = new();
}