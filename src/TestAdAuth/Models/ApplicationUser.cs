using Microsoft.AspNetCore.Identity;

namespace TestAdAuth.Models;

public class ApplicationUser : IdentityUser<Guid>
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? LdapDn { get; set; } // Distinguished Name в LDAP
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}