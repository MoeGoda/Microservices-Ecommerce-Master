using MediatR;
using Reporting.Application.Models;

namespace Reporting.Application.Features.Ingestion.Commands.IngestSaleCompleted
{
    // The receiving end of POS's SaleCompleted event (C3/D1) — property
    // names deliberately match POS's own SaleCompletedMessage exactly,
    // since ReportingEventPublisher (POS.Infrastructure) forwards that
    // event's PayloadJson to this endpoint VERBATIM, no shape translation
    // in between.
    public class IngestSaleCompletedCommand : IRequest<IngestResultDto>
    {
        public int SaleId { get; set; }
        public int LocationId { get; set; }
        public int CashierUserId { get; set; }
        public decimal Total { get; set; }
        public DateTime CompletedAtUtc { get; set; }
        public List<IngestSaleCompletedLine> Lines { get; set; } = new();
    }

    public class IngestSaleCompletedLine
    {
        public int ItemId { get; set; }
        public string Sku { get; set; } = null!;
        public string ItemName { get; set; } = null!;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal LineTotal { get; set; }
    }
}
