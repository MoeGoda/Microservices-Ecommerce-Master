using MediatR;
using Reporting.Application.Models;

namespace Reporting.Application.Features.Ingestion.Commands.IngestSaleReturned
{
    // POS's ReportingEventPublisher forwards the SaleReturned outbox
    // payload verbatim — the same SaleCompletedMessage shape
    // IngestSaleCompletedCommand binds — but this command only declares
    // SaleId, the only field it needs. System.Text.Json ignores the rest
    // of the payload's properties on bind, same as MVC model binding
    // would for any other unrecognized JSON field.
    public class IngestSaleReturnedCommand : IRequest<IngestResultDto>
    {
        public int SaleId { get; set; }
    }
}
