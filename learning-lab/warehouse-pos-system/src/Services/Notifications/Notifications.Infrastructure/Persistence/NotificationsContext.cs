using Microsoft.EntityFrameworkCore;
using Notifications.Domain.Entities;

namespace Notifications.Infrastructure.Persistence
{
    public class NotificationsContext : DbContext
    {
        public NotificationsContext(DbContextOptions<NotificationsContext> options) : base(options)
        {
        }

        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<StockLevelSnapshot> StockLevelSnapshots => Set<StockLevelSnapshot>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Notification>(builder =>
            {
                builder.Property(n => n.Type).HasConversion<string>().HasMaxLength(30);
                builder.Property(n => n.Message).HasMaxLength(500);

                // The idempotency guarantee IngestSaleCompletedCommandHandler's
                // ExistsForSale check relies on — a repeated delivery of the
                // same sale can never sneak past the application-layer check
                // and insert a second notification. Filtered so the many
                // LowStock rows (SourceSaleId always null) never collide.
                // No brackets around the column name — SQL Server accepts
                // an unquoted identifier here just as happily, and this
                // step's own runtime test applies the same model to SQLite
                // (D2 already hit one SQL-Server-only-translatable query;
                // an unquoted filter avoids the equivalent DDL portability
                // trap here).
                builder.HasIndex(n => n.SourceSaleId).IsUnique().HasFilter("SourceSaleId IS NOT NULL");

                // The feed's own access pattern — newest first, capped.
                builder.HasIndex(n => n.CreatedAt);
            });

            modelBuilder.Entity<StockLevelSnapshot>(builder =>
            {
                // Exactly one snapshot row per (ItemId, LocationId) —
                // IngestStockLevelChangedCommandHandler upserts against
                // this, the same "no dedup check needed, the unique index
                // plus a get-then-add/update is enough" shape D1's
                // StockLevelRecord (Reporting) already uses for the same
                // reason: it's a continuously-changing snapshot, not a
                // one-time fact.
                builder.HasIndex(s => new { s.ItemId, s.LocationId }).IsUnique();
            });
        }
    }
}
