using MediatR;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.Stock.Commands.TransferStock
{
    // Moving stock between two of THIS warehouse's own locations — not a
    // sale, not a purchase-order receipt, not a correction. Composed out
    // of the same StockAdjustmentStager.Stage(...) every other stock
    // command already uses, called twice (once negative at the source,
    // once positive at the destination) and committed once: the first
    // caller of the long-dead TransferIn/TransferOut reasons (see
    // StockTransactionReason's own comment).
    public class TransferStockCommand : IRequest<TransferStockResultDto>
    {
        public int ItemId { get; set; }
        public int FromLocationId { get; set; }
        public int ToLocationId { get; set; }
        public int Quantity { get; set; }
        public string? Reference { get; set; }
    }
}
