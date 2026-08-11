using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence
{
    public class IdentityContext : DbContext
    {
        public IdentityContext(DbContextOptions<IdentityContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(builder =>
            {
                builder.HasIndex(u => u.UserName).IsUnique();
                builder.HasIndex(u => u.Email).IsUnique();
                builder.Property(u => u.UserName).HasMaxLength(50);
                builder.Property(u => u.Email).HasMaxLength(256);

                builder.HasOne(u => u.Role)
                       .WithMany()
                       .HasForeignKey(u => u.RoleId)
                       .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Role>(builder =>
            {
                builder.HasIndex(r => r.Name).IsUnique();
                builder.Property(r => r.Name).HasMaxLength(50);
            });

            // Seeded via migration data, not runtime code, so the roles exist
            // atomically with the schema itself — no "did the seeder run
            // before the first request" race to worry about.
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = RoleNames.Admin, CreatedAt = SeedDate },
                new Role { Id = 2, Name = RoleNames.Manager, CreatedAt = SeedDate },
                new Role { Id = 3, Name = RoleNames.Cashier, CreatedAt = SeedDate },
                new Role { Id = 4, Name = RoleNames.WarehouseStaff, CreatedAt = SeedDate }
            );
        }

        // EF Core's HasData snapshot must be a fixed value, not DateTime.UtcNow
        // (which changes every time you regenerate a migration and would make
        // EF think the seed rows changed).
        private static readonly DateTime SeedDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    }
}
