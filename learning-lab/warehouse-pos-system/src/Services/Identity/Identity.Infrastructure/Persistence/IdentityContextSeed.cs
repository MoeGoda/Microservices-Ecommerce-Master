using Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence
{
    // Roles are seeded via EF migration HasData (IdentityContext), because
    // they're fixed reference data that should exist the instant the schema
    // does. The admin *user* is seeded here instead, at startup, because
    // creating it means hashing a password — a runtime operation, not
    // something you can bake into a migration's static HasData.
    public static class IdentityContextSeed
    {
        public static async Task SeedAdminUserAsync(IdentityContext context)
        {
            if (await context.Users.AnyAsync())
            {
                return;
            }

            var adminRole = await context.Roles.FirstAsync(r => r.Name == RoleNames.Admin);

            var admin = new User
            {
                UserName = "admin",
                Email = "admin@warehouse-pos.local",
                FirstName = "System",
                LastName = "Administrator",
                RoleId = adminRole.Id,
                Role = adminRole
            };

            // Same hasher the app uses at login time — a seed script that
            // hashed differently (or stored plaintext "for simplicity")
            // would silently fail to authenticate, or worse, would be a real
            // vulnerability if it ever shipped that way.
            var hasher = new PasswordHasher<User>();
            admin.PasswordHash = hasher.HashPassword(admin, "Admin@12345");

            context.Users.Add(admin);
            await context.SaveChangesAsync();
        }
    }
}
