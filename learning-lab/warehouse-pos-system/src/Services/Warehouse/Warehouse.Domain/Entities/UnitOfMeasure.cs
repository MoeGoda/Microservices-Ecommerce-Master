using Warehouse.Domain.Common;

namespace Warehouse.Domain.Entities
{
    // Master data — "what does this quantity number actually mean." PCS,
    // KG, BOX, and so on. Every Item picks one of these as its base unit
    // (Item.BaseUnitOfMeasure); everything else (ItemUnit conversions,
    // StockLevel, StockTransaction) is ultimately expressed in that base
    // unit, never left ambiguous.
    public class UnitOfMeasure : EntityBase
    {
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
    }
}
