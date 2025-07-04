using Microsoft.AspNetCore.Identity;

namespace TestAdAuth.Models;

public class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }
}