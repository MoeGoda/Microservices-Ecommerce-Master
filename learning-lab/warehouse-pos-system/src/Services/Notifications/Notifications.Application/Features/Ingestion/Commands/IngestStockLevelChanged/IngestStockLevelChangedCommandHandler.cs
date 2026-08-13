using MediatR;
using Notifications.Application.Contracts.Infrastructure;
using Notifications.Application.Contracts.Persistence;
using Notifications.Application.Models;
using Notifications.Domain.Entities;

namespace Notifications.Application.Features.Ingestion.Commands.IngestStockLevelChanged
{
    // Notifies only on the TRANSITION into low stock, not on every
    // qualifying event — StockLevelSnapshot (own tiny table, upserted
    // below on every event regardless of outcome) is what makes "was this
    // already low last time" answerable without asking Reporting, which
    // owns the real read model but isn't something Notifications should
    // ever query directly (separate services, separate databases). A
    // brand-new (ItemId, LocationId) with no snapshot yet always counts
    // as "wasn't low" — the first event Notifications ever sees for a
    // pair that arrives already low IS real news.
    public class IngestStockLevelChangedCommandHandler : IRequestHandler<IngestStockLevelChangedCommand, IngestResultDto>
    {
        private readonly IStockLevelSnapshotRepository _snapshotRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationPusher _notificationPusher;

        public IngestStockLevelChangedCommandHandler(
            IStockLevelSnapshotRepository snapshotRepository,
            INotificationRepository notificationRepository,
            IUnitOfWork unitOfWork,
            INotificationPusher notificationPusher)
        {
            _snapshotRepository = snapshotRepository;
            _notificationRepository = notificationRepository;
            _unitOfWork = unitOfWork;
            _notificationPusher = notificationPusher;
        }

        public async Task<IngestResultDto> Handle(IngestStockLevelChangedCommand request, CancellationToken cancellationToken)
        {
            var snapshot = await _snapshotRepository.GetByItemAndLocation(request.ItemId, request.LocationId);
            var wasLow = snapshot is not null && snapshot.QuantityOnHand <= snapshot.ReorderThreshold;
            var isLow = request.QuantityOnHand <= request.ReorderThreshold;

            // The snapshot always tracks the latest quantity/threshold,
            // whether or not this event ends up producing a notification —
            // otherwise the NEXT event would compare against stale data.
            if (snapshot is null)
            {
                await _snapshotRepository.AddAsync(new StockLevelSnapshot
                {
                    ItemId = request.ItemId,
                    LocationId = request.LocationId,
                    QuantityOnHand = request.QuantityOnHand,
                    ReorderThreshold = request.ReorderThreshold,
                });
            }
            else
            {
                snapshot.QuantityOnHand = request.QuantityOnHand;
                snapshot.ReorderThreshold = request.ReorderThreshold;
                await _snapshotRepository.UpdateAsync(snapshot);
            }

            if (!isLow || wasLow)
            {
                // Either not low right now, or already was low before
                // this event — nothing NEW to tell anyone about.
                await _unitOfWork.SaveChangesAsync();
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
