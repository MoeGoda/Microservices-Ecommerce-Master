using Warehouse.Domain.Common;

namespace Warehouse.Domain.Entities
{
    // The "inbox" side of POS's SaleReturned event — a SEPARATE table from
    // ProcessedSaleEvent, not a reused one, because a given SaleId
    // legitimately appears in BOTH: once when the original sale is
    // applied (decrement), once when it's returned (restock). Sharing one
    // table keyed only by SaleId would make the second insert collide
    // with the first. Same at-least-once-delivery reasoning as
    // ProcessedSaleEvent otherwise — a repeat SaleReturned delivery is a
    // no-op, not a second restock.
    public class ProcessedSaleReturnEvent : EntityBase
    {
        public int SaleId { get; set; }
    }
}
