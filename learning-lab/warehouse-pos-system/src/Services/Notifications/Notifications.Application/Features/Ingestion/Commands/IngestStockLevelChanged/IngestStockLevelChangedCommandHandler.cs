using MediatR;
using Notifications.Application.Contracts.Infrastructure;
using Notifications.Application.Contracts.Persistence;
using Notifications.Application.Models;
using Notifications.Domain.Entities;

namespace Notifications.Application.Features.Ingestion.Commands.IngestStockLevelChanged
{
    // Unlike IngestSaleCompletedCommandHandler, this one is NOT idempotent
    // against redelivery, and it's a deliberate, named gap rather than an
    // oversight: a low-stock notification fires on EVERY qualifying event,
    // not just the transition that first crosses the threshold. Detecting
    // "just crossed" would need Notifications to keep its own last-known
    // QuantityOnHand per (ItemId, LocationId) — effectively a second copy
    // of the read model Reporting (D1/D2) already owns — purely to
    // suppress its own duplicate alerts. Scoped out for this step; a real
    // deployment would want that dedup (or a client-side debounce) before
    // this could safely fire on every stock adjustment to an already-low
    // item without becoming noise.
    public class IngestStockLevelChangedCommandHandler : IRequestHandler<IngestStockLevelChangedCommand, IngestResultDto>
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationPusher _notificationPusher;

        public IngestStockLevelChangedCommandHandler(
            INotificationRepository notificationRepository,
            IUnitOfWork unitOfWork,
            INotificationPusher notificationPusher)
        {
            _notificationRepository = notificationRepository;
            _unitOfWork = unitOfWork;
            _notificationPusher = notificationPusher;
        }

        public async Task<IngestResultDto> Handle(IngestStockLevelChangedCommand request, CancellationToken cancellationToken)
        {
            if (request.QuantityOnHand > request.ReorderThreshold)
            {
                // Not low stock — nothing to notify about, and nothing
                // was persisted, so there's no "already processed" case
                // to report either.
                return new IngestResultDto { AlreadyProcessed = false };
            }

            var notification = await _notificationRepository.AddAsync(new Notification
            {
                Type = NotificationType.LowStock,
                Message = $"Low stock: {request.ItemName} ({request.Sku}) at {request.LocationName} — {request.QuantityOnHand} on hand, reorder threshold {request.ReorderThreshold}.",
            });

            await _unitOfWork.SaveChangesAsync();

            await _notificationPusher.PushAsync(NotificationDto.FromEntity(notification), cancellationToken);

            return new IngestResultDto { AlreadyProcessed = false };
        }
    }
}
