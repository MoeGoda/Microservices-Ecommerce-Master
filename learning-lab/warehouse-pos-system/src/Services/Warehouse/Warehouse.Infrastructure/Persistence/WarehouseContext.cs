using Microsoft.EntityFrameworkCore;
using Warehouse.Domain.Entities;

namespace Warehouse.Infrastructure.Persistence
{
    public class WarehouseContext : DbContext
    {
        public WarehouseContext(DbContextOptions<WarehouseContext> options) : base(options)
        {
        }

        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Location> Locations => Set<Location>();
        public DbSet<Item> Items => Set<Item>();
        public DbSet<StockLevel> StockLevels => Set<StockLevel>();
        public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>(builder =>
            {
                builder.HasIndex(c => c.Name).IsUnique();
                builder.Property(c => c.Name).HasMaxLength(100);
            });

            modelBuilder.Entity<Location>(builder =>
            {
                builder.HasIndex(l => l.Code).IsUnique();
                builder.Property(l => l.Code).HasMaxLength(20);
                builder.Property(l => l.Name).HasMaxLength(100);
            });

            modelBuilder.Entity<Item>(builder =>
            {
                // The one constraint this whole module exists to enforce:
                // a barcode identifies exactly one item, always.
                builder.HasIndex(i => i.Barcode).IsUnique();
                builder.Property(i => i.Barcode).HasMaxLength(50);
                builder.Property(i => i.Name).HasMaxLength(200);
                builder.Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");

                builder.HasOne(i => i.Category)
                       .WithMany()
                       .HasForeignKey(i => i.CategoryId)
                       .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<StockLevel>(builder =>
            {
                // Exactly one balance row per item+location — never two
                // competing "current quantity" rows for the same pair.
                builder.HasIndex(s => new { s.ItemId, s.LocationId }).IsUnique();

                builder.HasOne(s => s.Item)
                       .WithMany()
                       .HasForeignKey(s => s.ItemId)
                       .OnDelete(DeleteBehavior.Restrict);

                builder.HasOne(s => s.Location)
                       .WithMany()
                       .HasForeignKey(s => s.LocationId)
                       .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<StockTransaction>(builder =>
            {
                builder.Property(t => t.Reference).HasMaxLength(100);

                builder.HasOne(t => t.Item)
                       .WithMany()
                       .HasForeignKey(t => t.ItemId)
                       .OnDelete(DeleteBehavior.Restrict);

                builder.HasOne(t => t.Location)
                       .WithMany()
                       .HasForeignKey(t => t.LocationId)
                       .OnDelete(DeleteBehavior.Restrict);
            });

            // Categories and Locations are fixed reference/master data —
            // seeded via migration HasData so they exist the instant the
            // schema does, the same reasoning as Identity's seeded Roles.
            // Items and StockLevels are sample data, not reference data
            // (a real deployment starts with zero items), so those are
            // seeded at runtime instead — see WarehouseContextSeed.
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Beverages", CreatedAt = SeedDate },
                new Category { Id = 2, Name = "Snacks", CreatedAt = SeedDate },
                new Category { Id = 3, Name = "Household", CreatedAt = SeedDate }
            );

            modelBuilder.Entity<Location>().HasData(
                new Location { Id = 1, Code = "A1", Name = "Aisle A, Shelf 1", CreatedAt = SeedDate },
                new Location { Id = 2, Code = "A2", Name = "Aisle A, Shelf 2", CreatedAt = SeedDate },
                new Location { Id = 3, Code = "B1", Name = "Aisle B, Shelf 1", CreatedAt = SeedDate }
            );
        }

        private static readonly DateTime SeedDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    }
}
