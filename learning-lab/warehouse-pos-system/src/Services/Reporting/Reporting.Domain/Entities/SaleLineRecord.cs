using Reporting.Domain.Common;

namespace Reporting.Domain.Entities
{
    // One row per line of a SaleRecord — split out rather than a
    // LinesJson blob (unlike POS's own outbox message, which never
    // queries into individual lines) because D2's "top-selling items"
    // report needs to group and sum ACROSS sales by ItemId, which a JSON
    // blob can't do without deserializing every row first.
    public class SaleLineRecord : EntityBase
    {
        public int SaleId { get; set; }
        public int ItemId { get; set; }
        public string Sku { get; set; } = null!;
        public string ItemName { get; set; } = null!;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal LineTotal { get; set; }
    }
}
