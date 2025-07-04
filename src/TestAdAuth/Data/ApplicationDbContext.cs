using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TestAdAuth.Models;

namespace TestAdAuth.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Seed roles
        builder.Entity<ApplicationRole>().HasData(
            new ApplicationRole 
            { 
                Id = new Guid("37d98be3-d090-4aba-86dc-6a5c9df87092"), 
                Name = "Employee", 
                NormalizedName = "EMPLOYEE",
                Description = "Сотрудник"
            },
            new ApplicationRole 
            { 
                Id = new Guid("64e60339-f57d-4edd-bbca-484f334ab058"),
                Name = "OfficeManager", 
                NormalizedName = "OFFICEMANAGER",
                Description = "Офис-менеджер"
            }
        );
    }
}