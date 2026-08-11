using Microsoft.EntityFrameworkCore;
using Warehouse.Domain.Entities;

namespace Warehouse.Infrastructure.Persistence
{
    // Sample items with realistic-looking EAN-13 barcodes, so there's
    // something to actually scan/look up immediately — the same reasoning
    // as MenuItemsContextSeed back in the first learning-lab, and as
    // IdentityContext's runtime-seeded admin user: this is demo data, not
    // fixed reference data, so it's seeded here rather than via migration
    // HasData (see WarehouseContext for that distinction).
    public static class WarehouseContextSeed
    {
        public static async Task SeedSampleItemsAsync(WarehouseContext context)
        {
            if (await context.Items.AnyAsync())
            {
                return;
            }

            var beverages = await context.Categories.FirstAsync(c => c.Name == "Beverages");
            var snacks = await context.Categories.FirstAsync(c => c.Name == "Snacks");
            var shelfA1 = await context.Locations.FirstAsync(l => l.Code == "A1");

            var items = new[]
            {
                new Item { Name = "Cola 330ml Can", Barcode = "5901234123457", UnitPrice = 1.80m, CategoryId = beverages.Id, Category = beverages },
                new Item { Name = "Sparkling Water 500ml", Barcode = "5901234123464", UnitPrice = 1.20m, CategoryId = beverages.Id, Category = beverages },
                new Item { Name = "Potato Chips 150g", Barcode = "5901234123471", UnitPrice = 2.50m, CategoryId = snacks.Id, Category = snacks },
            };

            context.Items.AddRange(items);
            await context.SaveChangesAsync();

            foreach (var item in items)
            {
                context.StockLevels.Add(new StockLevel
                {
                    ItemId = item.Id,
                    LocationId = shelfA1.Id,
                    QuantityOnHand = 50,
                    ReorderThreshold = 10,
                });
            }

            await context.SaveChangesAsync();
        }
    }
}
