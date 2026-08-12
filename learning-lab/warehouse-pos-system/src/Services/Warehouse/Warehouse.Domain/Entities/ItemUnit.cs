using Warehouse.Domain.Common;

namespace Warehouse.Domain.Entities
{
    // An *alternate* unit an item can be received or sold in, besides its
    // base unit — "this item's base unit is PCS, but it can also be
    // received by the BOX, where 1 BOX = 12 PCS." There's deliberately no
    // row here for the base unit itself (that's just Item.BaseUnitOfMeasure,
    // implicitly a conversion factor of 1) — this table only exists for
    // units that need converting *into* the base unit before they touch
    // inventory.
    public class ItemUnit : EntityBase
    {
        public int ItemId { get; set; }
        public Item Item { get; set; } = null!;

        public int UnitOfMeasureId { get; set; }
        public UnitOfMeasure UnitOfMeasure { get; set; } = null!;

        // How many of the item's BASE unit one of THIS unit equals.
        // E.g. UnitOfMeasure = BOX, ConversionFactor = 12 means "1 BOX of
        // this item = 12 of its base unit." Decimal, not int, because not
        // every real conversion is a whole number (e.g. weight-based units).
        public decimal ConversionFactor { get; set; }
    }
}
