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
        public string Sku { get; set; } = null!;
        public string ItemName { get; set; } = null!;
        public int LocationId { get; set; }
        public string LocationCode { get; set; } = null!;
        public string LocationName { get; set; } = null!;
        public int QuantityOnHand { get; set; }
        public int ReorderThreshold { get; set; }
    }
}
