using Microsoft.EntityFrameworkCore;
using Reporting.Domain.Entities;

namespace Reporting.Infrastructure.Persistence
{
    public class ReportingContext : DbContext
    {
        public ReportingContext(DbContextOptions<ReportingContext> options) : base(options)
        {
        }

        public DbSet<SaleRecord> SaleRecords => Set<SaleRecord>();
        public DbSet<SaleLineRecord> SaleLineRecords => Set<SaleLineRecord>();
        public DbSet<StockLevelRecord> StockLevelRecords => Set<StockLevelRecord>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SaleRecord>(builder =>
            {
                // The idempotency guarantee IngestSaleCompletedCommandHandler's
                // ExistsForSale check relies on — a repeated delivery of the
                // same sale can never sneak past the application-layer check
                // and insert twice.
                builder.HasIndex(r => r.SaleId).IsUnique();
                builder.Property(r => r.Total).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<SaleLineRecord>(builder =>
            {
                builder.Property(l => l.Sku).HasMaxLength(50);
                builder.Property(l => l.ItemName).HasMaxLength(200);
                builder.Property(l => l.UnitPrice).HasColumnType("decimal(18,2)");
                builder.Property(l => l.LineTotal).HasColumnType("decimal(18,2)");

                // Indexed for D2's "top-selling items" report — grouping
                // by ItemId across every SaleLineRecord is the exact
                // access pattern that report needs.
                builder.HasIndex(l => l.ItemId);
                builder.HasIndex(l => l.SaleId);
            });

            modelBuilder.Entity<StockLevelRecord>(builder =>
            {
                // Exactly one snapshot row per (ItemId, LocationId) —
                // IngestStockLevelChangedCommandHandler upserts against
                // this, never inserts a second row for the same pair.
                builder.HasIndex(r => new { r.ItemId, r.LocationId }).IsUnique();
            });
        }
    }
}
