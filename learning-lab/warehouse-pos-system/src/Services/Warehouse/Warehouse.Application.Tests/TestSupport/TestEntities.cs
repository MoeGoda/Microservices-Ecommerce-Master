using Warehouse.Domain.Entities;

namespace Warehouse.Application.Tests.TestSupport
{
    // Plain object-mother helpers for the entities every handler test needs
    // wired up (an Item with a base UnitOfMeasure, a couple of Locations).
    // Not test cases themselves — just constructors with sane defaults so
    // each test only has to override what it actually cares about.
    internal static class TestEntities
    {
        public static UnitOfMeasure UnitOfMeasure(int id = 1, string code = "PCS", string name = "Pieces") =>
            new() { Id = id, Code = code, Name = name };

        public static Category Category(int id = 1, string name = "General") =>
            new() { Id = id, Name = name };

        public static Item Item(int id = 1, string sku = "SKU-1", UnitOfMeasure? baseUnit = null, decimal unitPrice = 10m)
        {
            var unit = baseUnit ?? UnitOfMeasure();
            return new Item
            {
                Id = id,
                Sku = sku,
                Name = $"Item {sku}",
                UnitPrice = unitPrice,
                CategoryId = 1,
                Category = Category(),
                BaseUnitOfMeasureId = unit.Id,
                BaseUnitOfMeasure = unit,
            };
        }

        public static Location Location(int id = 1, string code = "A1", string name = "Aisle 1") =>
            new() { Id = id, Code = code, Name = name };

        public static ItemUnit ItemUnit(Item item, UnitOfMeasure unit, decimal conversionFactor) =>
            new()
            {
                Id = 1,
                ItemId = item.Id,
                Item = item,
                UnitOfMeasureId = unit.Id,
                UnitOfMeasure = unit,
                ConversionFactor = conversionFactor,
            };

        public static StockLevel StockLevel(Item item, Location location, int quantityOnHand, int reorderThreshold = 0) =>
            new()
            {
                Id = 1,
                ItemId = item.Id,
                Item = item,
                LocationId = location.Id,
                Location = location,
                QuantityOnHand = quantityOnHand,
                ReorderThreshold = reorderThreshold,
                UnitOfMeasureId = item.BaseUnitOfMeasureId,
                UnitOfMeasure = item.BaseUnitOfMeasure,
            };

        public static Supplier Supplier(int id = 1, string name = "Acme Supplies", bool isActive = true) =>
            new() { Id = id, Name = name, IsActive = isActive };
    }
}
