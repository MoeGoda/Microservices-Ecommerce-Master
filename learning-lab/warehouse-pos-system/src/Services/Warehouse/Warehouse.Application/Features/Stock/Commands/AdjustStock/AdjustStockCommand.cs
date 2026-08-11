using MediatR;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.Stock.Commands.AdjustStock
{
    // A manual correction (damaged goods, cycle-count correction, theft) —
    // always in the item's base unit, always signed (+/-), and, unlike
    // ReceiveStockCommand, never creates a StockLevel that doesn't already
    // exist: "adjusting" a balance implies there's a balance to adjust.
    // Recorded as StockTransactionReason.Adjustment; that reason is the
    // command's own intent, not something the caller chooses.
    public class AdjustStockCommand : IRequest<StockLevelDto>
    {
        public int ItemId { get; set; }
        public int LocationId { get; set; }
        public int QuantityChange { get; set; }
        public string? Reference { get; set; }
    }
}
