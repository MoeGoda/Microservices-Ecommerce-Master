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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Sale>(builder =>
            {
                builder.Property(s => s.Total).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<SaleLine>(builder =>
            {
                builder.Property(l => l.Sku).HasMaxLength(50);
                builder.Property(l => l.ItemName).HasMaxLength(200);
                builder.Property(l => l.UnitPrice).HasColumnType("decimal(18,2)");
                builder.Property(l => l.LineTotal).HasColumnType("decimal(18,2)");

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
