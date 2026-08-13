using MediatR;
using Notifications.Application.Models;

namespace Notifications.Application.Features.Ingestion.Commands.IngestStockLevelChanged
{
    // The receiving end of Warehouse's StockLevelChanged event (D1/D2's
    // enriched shape) — property names match StockLevelChangedMessage so
    // Warehouse's NotificationsEventPublisher can forward the outbox's
    // PayloadJson verbatim, same idiom as IngestSaleCompletedCommand.
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
