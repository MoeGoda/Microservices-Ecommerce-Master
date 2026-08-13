using MediatR;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.Stock.Commands.ApplySaleReturn
{
    // What POS's outbox dispatcher sends when a completed Sale is
    // returned — "restock every line of this sale, at this location, or
    // none of them." SaleId is the idempotency key (see
    // ProcessedSaleReturnEvent), a SEPARATE key space from ApplySaleCommand's
    // ProcessedSaleEvent even though both are keyed by the same SaleId — see
    // that entity's own comment for why.
    public class ApplySaleReturnCommand : IRequest<ApplySaleResultDto>
    {
        public int SaleId { get; set; }
        public int LocationId { get; set; }
        public List<ApplySaleReturnLine> Lines { get; set; } = new();
    }

    public class ApplySaleReturnLine
    {
        public int ItemId { get; set; }
        public int Quantity { get; set; }
    }
}
