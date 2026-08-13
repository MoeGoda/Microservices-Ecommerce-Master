using MediatR;
using Notifications.Application.Models;

namespace Notifications.Application.Features.Ingestion.Commands.IngestSaleReturned
{
    // The receiving end of POS's SaleReturned event, same forwarded
    // SaleCompletedMessage shape as IngestSaleCompletedCommand — only
    // SaleId and Total are bound, the same "less to bind, not a shape
    // translation" reasoning that command's own comment already gives.
    public class IngestSaleReturnedCommand : IRequest<IngestResultDto>
    {
        public int SaleId { get; set; }
        public decimal Total { get; set; }
    }
}
