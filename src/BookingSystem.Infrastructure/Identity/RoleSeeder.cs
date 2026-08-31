using BookingSystem.Domain.Constants;
using Microsoft.AspNetCore.Identity;

namespace BookingSystem.Infrastructure.Identity;

public static class RoleSeeder
{
    public static async Task SeedAsync(RoleManager<IdentityRole> roleManager)
    {
        string[] roles = [Roles.Admin, Roles.User];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }
}
