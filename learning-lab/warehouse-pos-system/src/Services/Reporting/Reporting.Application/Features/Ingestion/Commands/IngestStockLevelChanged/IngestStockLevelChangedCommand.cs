using MediatR;
using Reporting.Application.Models;

namespace Reporting.Application.Features.Ingestion.Commands.IngestStockLevelChanged
{
    // Property names match Warehouse's own StockLevelChangedMessage
    // exactly — ReportingEventPublisher (Warehouse.Infrastructure)
    // forwards that event's PayloadJson to this endpoint verbatim.
    public class IngestStockLevelChangedCommand : IRequest<IngestResultDto>
    {
        public int ItemId { get; set; }
        public int LocationId { get; set; }
        public int QuantityOnHand { get; set; }
    }
}
