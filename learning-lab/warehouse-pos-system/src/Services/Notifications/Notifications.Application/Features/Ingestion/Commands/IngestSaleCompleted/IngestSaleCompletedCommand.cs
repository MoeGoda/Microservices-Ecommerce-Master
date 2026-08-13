using MediatR;
using Notifications.Application.Models;

namespace Notifications.Application.Features.Ingestion.Commands.IngestSaleCompleted
{
    // The receiving end of POS's SaleCompleted event (C3/D1), same event
    // POS's own NotificationsEventPublisher forwards verbatim from its
    // outbox — but unlike Reporting's identically-named command, this one
    // only declares the two fields a notification message actually needs.
    // Model binding ignores the rest of the JSON (LocationId, Lines, …)
    // without complaint; there's no shape translation to write for that,
    // just less to bind.
    public class IngestSaleCompletedCommand : IRequest<IngestResultDto>
    {
        public int SaleId { get; set; }
        public decimal Total { get; set; }
    }
}
