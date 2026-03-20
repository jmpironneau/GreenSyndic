using GreenSyndic.Core.Entities;
using GreenSyndic.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GreenSyndic.Infrastructure.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GreenSyndicDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await db.Database.MigrateAsync();

        // Create roles
        string[] roles = ["SuperAdmin", "SyndicManager", "SyndicAccountant", "SyndicTechnician",
            "CouncilPresident", "CouncilMember", "Owner", "Tenant", "CommercialTenant",
            "Supplier", "SecurityAgent", "ReadOnly"];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Create default organization (Green City Bassam)
        if (!await db.Organizations.AnyAsync())
        {
            var org = new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Green City Bassam",
                LegalName = "COFIPRI - Green City Bassam",
                Country = "CI",
                Currency = "XOF",
                City = "Grand Bassam",
                Address = "Grand Bassam, Côte d'Ivoire",
                IsActive = true
            };
            db.Organizations.Add(org);
            await db.SaveChangesAsync();

            // Create admin user
            var adminEmail = "admin@greensyndic.ci";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Admin",
                    LastName = "GreenSyndic",
                    OrganizationId = org.Id,
                    ProfileRole = "SuperAdmin",
                    IsActive = true,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(admin, "Admin@2026!");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(admin, "SuperAdmin");
            }
        }
    }
}
