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
            var pcs = await context.UnitsOfMeasure.FirstAsync(u => u.Code == "PCS");
            var box = await context.UnitsOfMeasure.FirstAsync(u => u.Code == "BOX");

            var cola = new Item { Sku = "BEV-COLA-330", Name = "Cola 330ml Can", UnitPrice = 1.80m, CategoryId = beverages.Id, Category = beverages, BaseUnitOfMeasureId = pcs.Id, BaseUnitOfMeasure = pcs };
            var water = new Item { Sku = "BEV-WATER-500", Name = "Sparkling Water 500ml", UnitPrice = 1.20m, CategoryId = beverages.Id, Category = beverages, BaseUnitOfMeasureId = pcs.Id, BaseUnitOfMeasure = pcs };
            var chips = new Item { Sku = "SNK-CHIPS-150", Name = "Potato Chips 150g", UnitPrice = 2.50m, CategoryId = snacks.Id, Category = snacks, BaseUnitOfMeasureId = pcs.Id, BaseUnitOfMeasure = pcs };

            var items = new[] { cola, water, chips };
            context.Items.AddRange(items);
            await context.SaveChangesAsync();

            // Cola ships this quarter with two valid barcodes — the
            // manufacturer's own, and a relabeled supplier variant — both
            // resolving to the same item and the same shared stock count.
            // This is the concrete case the single-barcode design couldn't
            // represent.
            context.ItemBarcodes.AddRange(
                new ItemBarcode { ItemId = cola.Id, Barcode = "5901234123457", BarcodeType = BarcodeType.EAN13, IsPrimary = true },
                new ItemBarcode { ItemId = cola.Id, Barcode = "5901234199999", BarcodeType = BarcodeType.EAN13, IsPrimary = false },
                new ItemBarcode { ItemId = water.Id, Barcode = "5901234123464", BarcodeType = BarcodeType.EAN13, IsPrimary = true },
                new ItemBarcode { ItemId = chips.Id, Barcode = "5901234123471", BarcodeType = BarcodeType.EAN13, IsPrimary = true }
            );

            // Cola is also received by the box — 1 BOX = 12 PCS. A
            // "receive 2 BOX" operation (Step B2) converts through this
            // before it ever touches StockLevel/StockTransaction, which
            // only ever speak PCS for this item.
            context.ItemUnits.Add(new ItemUnit { ItemId = cola.Id, UnitOfMeasureId = box.Id, ConversionFactor = 12m });

            await context.SaveChangesAsync();

            foreach (var item in items)
            {
                context.StockLevels.Add(new StockLevel
                {
                    ItemId = item.Id,
                    LocationId = shelfA1.Id,
                    QuantityOnHand = 50,
                    ReorderThreshold = 10,
                    UnitOfMeasureId = item.BaseUnitOfMeasureId,
                });
            }

            await context.SaveChangesAsync();
        }
    }
}
