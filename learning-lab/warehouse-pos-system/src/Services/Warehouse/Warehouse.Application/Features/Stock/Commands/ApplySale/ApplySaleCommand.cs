using MediatR;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.Stock.Commands.ApplySale
{
    // What POS's outbox dispatcher (C3) sends when a Sale completes —
    // "decrement stock for every line of this sale, at this location, or
    // none of them." SaleId is the idempotency key (see ProcessedSaleEvent):
    // at-least-once delivery means this exact command can arrive more than
    // once for the same sale, and it has to be safe to receive it twice.
    public class ApplySaleCommand : IRequest<ApplySaleResultDto>
    {
        public int SaleId { get; set; }
        public int LocationId { get; set; }
        public List<ApplySaleLine> Lines { get; set; } = new();
    }

    public class ApplySaleLine
    {
        public int ItemId { get; set; }
        public int Quantity { get; set; }
    }
}
