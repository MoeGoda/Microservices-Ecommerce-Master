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
        public DbSet<UnitOfMeasure> UnitsOfMeasure => Set<UnitOfMeasure>();
        public DbSet<Item> Items => Set<Item>();
        public DbSet<ItemBarcode> ItemBarcodes => Set<ItemBarcode>();
        public DbSet<ItemUnit> ItemUnits => Set<ItemUnit>();
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

            modelBuilder.Entity<UnitOfMeasure>(builder =>
            {
                builder.HasIndex(u => u.Code).IsUnique();
                builder.Property(u => u.Code).HasMaxLength(10);
                builder.Property(u => u.Name).HasMaxLength(50);
            });

            modelBuilder.Entity<Item>(builder =>
            {
                // Sku is the item's own identity now — barcodes live in
                // ItemBarcode instead, precisely so an item isn't limited
                // to exactly one of them.
                builder.HasIndex(i => i.Sku).IsUnique();
                builder.Property(i => i.Sku).HasMaxLength(50);
                builder.Property(i => i.Name).HasMaxLength(200);
                builder.Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");

                builder.HasOne(i => i.Category)
                       .WithMany()
                       .HasForeignKey(i => i.CategoryId)
                       .OnDelete(DeleteBehavior.Restrict);

                // Restrict, not Cascade: a UnitOfMeasure that items still
                // reference shouldn't be deletable out from under them.
                builder.HasOne(i => i.BaseUnitOfMeasure)
                       .WithMany()
                       .HasForeignKey(i => i.BaseUnitOfMeasureId)
                       .OnDelete(DeleteBehavior.Restrict);

                // Self-referencing, optional: a pack/variant Item points
                // back at its base product. Restrict so a base product
                // with existing pack variants can't be deleted out from
                // under them — that has to be an explicit decision made
                // somewhere in B2, not something a delete silently cascades.
                builder.HasOne(i => i.ParentItem)
                       .WithMany()
                       .HasForeignKey(i => i.ParentItemId)
                       .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ItemBarcode>(builder =>
            {
                // A barcode identifies exactly one item, full stop — this
                // is the constraint that used to live on Item.Barcode.
                builder.HasIndex(b => b.Barcode).IsUnique();
                builder.Property(b => b.Barcode).HasMaxLength(50);

                // At most one PRIMARY barcode per item — a *filtered*
                // unique index (only rows where IsPrimary = 1 participate),
                // not a plain unique index on ItemId, because an item is
                // allowed many non-primary barcodes; it just can't have
                // two that both claim to be "the" primary one.
                builder.HasIndex(b => b.ItemId)
                       .IsUnique()
                       .HasFilter("[IsPrimary] = 1")
                       .HasDatabaseName("IX_ItemBarcodes_ItemId_Primary");

                // Cascade here (unlike Item's other relationships): a
                // barcode has no meaning independent of its item, so
                // deleting the item should take its barcodes with it.
                builder.HasOne(b => b.Item)
                       .WithMany()
                       .HasForeignKey(b => b.ItemId)
                       .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ItemUnit>(builder =>
            {
                // An item can't define the same alternate unit twice.
                builder.HasIndex(u => new { u.ItemId, u.UnitOfMeasureId }).IsUnique();
                builder.Property(u => u.ConversionFactor).HasColumnType("decimal(18,4)");

                builder.HasOne(u => u.Item)
                       .WithMany()
                       .HasForeignKey(u => u.ItemId)
                       .OnDelete(DeleteBehavior.Cascade);

                builder.HasOne(u => u.UnitOfMeasure)
                       .WithMany()
                       .HasForeignKey(u => u.UnitOfMeasureId)
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

                builder.HasOne(s => s.UnitOfMeasure)
                       .WithMany()
                       .HasForeignKey(s => s.UnitOfMeasureId)
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

            // Categories, Locations, and Units of Measure are fixed
            // reference/master data — seeded via migration HasData so
            // they exist the instant the schema does, the same reasoning
            // as Identity's seeded Roles. Items, their barcodes/units, and
            // StockLevels are sample data, not reference data (a real
            // deployment starts with zero items), so those are seeded at
            // runtime instead — see WarehouseContextSeed.
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

            modelBuilder.Entity<UnitOfMeasure>().HasData(
                new UnitOfMeasure { Id = 1, Code = "PCS", Name = "Pieces", CreatedAt = SeedDate },
                new UnitOfMeasure { Id = 2, Code = "KG", Name = "Kilogram", CreatedAt = SeedDate },
                new UnitOfMeasure { Id = 3, Code = "BOX", Name = "Box", CreatedAt = SeedDate },
                new UnitOfMeasure { Id = 4, Code = "CARTON", Name = "Carton", CreatedAt = SeedDate },
                new UnitOfMeasure { Id = 5, Code = "LITER", Name = "Liter", CreatedAt = SeedDate }
            );
        }

        private static readonly DateTime SeedDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    }
}
