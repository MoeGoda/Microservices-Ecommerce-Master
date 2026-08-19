using Microsoft.EntityFrameworkCore;
using POS.Domain.Entities;

namespace POS.Infrastructure.Persistence
{
    public class PosContext : DbContext
    {
        public PosContext(DbContextOptions<PosContext> options) : base(options)
        {
        }

        public DbSet<Sale> Sales => Set<Sale>();
        public DbSet<SaleLine> SaleLines => Set<SaleLine>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
        public DbSet<OutboxDelivery> OutboxDeliveries => Set<OutboxDelivery>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<CashDrawerSession> CashDrawerSessions => Set<CashDrawerSession>();
        public DbSet<CashMovement> CashMovements => Set<CashMovement>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Sale>(builder =>
            {
                builder.Property(s => s.Total).HasColumnType("decimal(18,2)");
                builder.Property(s => s.NetTotal).HasColumnType("decimal(18,2)");
                builder.Property(s => s.TaxAmount).HasColumnType("decimal(18,2)");
                builder.Property(s => s.ManualReceiptDiscountPercent).HasColumnType("decimal(5,2)");

                // Restrict, not Cascade/SetNull: a completed sale's
                // history has to survive independent of the Customer row
                // it was rung up against — the same "history outlives the
                // master record" reasoning StockLevel/StockTransaction's
                // Restrict relationship to Item already uses in
                // Warehouse. Deleting a Customer with sales on file is
                // blocked, not silently unlinked.
                builder.HasOne(s => s.Customer)
                       .WithMany()
                       .HasForeignKey(s => s.CustomerId)
                       .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Customer>(builder =>
            {
                builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
                builder.Property(c => c.Phone).HasMaxLength(30);
                builder.Property(c => c.Email).HasMaxLength(200);
                builder.Property(c => c.Balance).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<CashDrawerSession>(builder =>
            {
                builder.Property(s => s.OpeningFloat).HasColumnType("decimal(18,2)");
                builder.Property(s => s.ClosingCount).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<CashMovement>(builder =>
            {
                builder.Property(m => m.Amount).HasColumnType("decimal(18,2)");
                builder.Property(m => m.Reason).HasMaxLength(200);

                // A movement has no meaning independent of the session it
                // was recorded against — same cascade reasoning as
                // SaleLine's relationship to Sale below.
                builder.HasOne(m => m.CashDrawerSession)
                       .WithMany()
                       .HasForeignKey(m => m.CashDrawerSessionId)
                       .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<OutboxMessage>(builder =>
            {
                builder.Property(m => m.EventType).HasMaxLength(100);
                builder.Property(m => m.PayloadJson).IsRequired();
            });

            modelBuilder.Entity<OutboxDelivery>(builder =>
            {
                builder.Property(d => d.ConsumerName).HasMaxLength(100);
                builder.Property(d => d.LastError).HasMaxLength(1000);

                // A message has no meaning independent of its deliveries
                // once both exist — same cascade reasoning as SaleLine's
                // relationship to Sale below.
                builder.HasOne(d => d.OutboxMessage)
                       .WithMany()
                       .HasForeignKey(d => d.OutboxMessageId)
                       .OnDelete(DeleteBehavior.Cascade);

                // Never two deliveries for the same (message, consumer)
                // pair — CheckoutCommandHandler creates exactly one row
                // per consumer per message, and this is what would catch
                // it if that ever accidentally ran twice.
                builder.HasIndex(d => new { d.OutboxMessageId, d.ConsumerName }).IsUnique();
            });

            modelBuilder.Entity<SaleLine>(builder =>
            {
                builder.Property(l => l.Sku).HasMaxLength(50);
                builder.Property(l => l.ItemName).HasMaxLength(200);
                builder.Property(l => l.UnitPrice).HasColumnType("decimal(18,2)");
                builder.Property(l => l.OriginalUnitPrice).HasColumnType("decimal(18,2)");
                builder.Property(l => l.LineTotal).HasColumnType("decimal(18,2)");
                builder.Property(l => l.ManualDiscountPercent).HasColumnType("decimal(5,2)");

                // Cascade, not Restrict: a SaleLine has no meaning
                // independent of its Sale (same reasoning as
                // ItemBarcode/ItemUnit cascading from Item in Warehouse,
                // B1) — unlike StockLevel/StockTransaction's relationship
                // to Item, which is Restrict because that history has to
                // survive independent of any one Item row.
                builder.HasOne(l => l.Sale)
                       .WithMany()
                       .HasForeignKey(l => l.SaleId)
                       .OnDelete(DeleteBehavior.Cascade);
            });

            // No HasData here — unlike Warehouse's Category/Location/UnitOfMeasure,
            // POS has no fixed reference data of its own. Every Sale is
            // transactional, created at runtime; there is nothing to seed.
        }
    }
}
