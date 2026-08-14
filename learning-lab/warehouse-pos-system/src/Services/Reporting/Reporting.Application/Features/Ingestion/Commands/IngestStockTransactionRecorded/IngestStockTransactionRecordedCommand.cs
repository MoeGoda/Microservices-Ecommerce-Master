using MediatR;
using Reporting.Application.Models;

namespace Reporting.Application.Features.Ingestion.Commands.IngestStockTransactionRecorded
{
    // Property names match Warehouse's own StockTransactionRecordedMessage
    // exactly — ReportingEventPublisher (Warehouse.Infrastructure)
    // forwards that event's PayloadJson to this endpoint verbatim, the
    // same convention IngestStockLevelChangedCommand already follows.
    public class IngestStockTransactionRecordedCommand : IRequest<IngestResultDto>
    {
        public int ItemId { get; set; }
        public string Sku { get; set; } = null!;
        public string ItemName { get; set; } = null!;
        public int LocationId { get; set; }
        public string LocationCode { get; set; } = null!;
        public string LocationName { get; set; } = null!;
        public int QuantityChange { get; set; }
        public string Reason { get; set; } = null!;
        public string? Reference { get; set; }
        public DateTime TransactionAtUtc { get; set; }
    }
}
