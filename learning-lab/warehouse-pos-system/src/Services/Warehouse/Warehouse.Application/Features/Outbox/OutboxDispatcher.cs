using Warehouse.Application.Contracts.Infrastructure;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Features.Outbox
{
    // Same shape as POS's OutboxDispatcher (C3/D1) — "for every
    // undelivered delivery, find the publisher for its ConsumerName, try
    // to deliver it, record what happened" — but genuinely generic here
    // with no legacy per-consumer side effect to special-case, since
    // Warehouse has no entity analogous to Sale.StockSyncStatus that a
    // delivery outcome needs to update.
    public class OutboxDispatcher
    {
        private const int MaxAttempts = 5;

        private readonly IOutboxRepository _outboxRepository;
        private readonly IEnumerable<IEventPublisher> _publishers;
        private readonly IUnitOfWork _unitOfWork;

        public OutboxDispatcher(IOutboxRepository outboxRepository, IEnumerable<IEventPublisher> publishers, IUnitOfWork unitOfWork)
        {
            _outboxRepository = outboxRepository;
            _publishers = publishers;
            _unitOfWork = unitOfWork;
        }

        public async Task DispatchPendingAsync(CancellationToken cancellationToken)
        {
            var deliveries = await _outboxRepository.GetPendingDeliveries();
            foreach (var delivery in deliveries)
            {
                await DispatchOne(delivery, cancellationToken);
            }
        }

        private async Task DispatchOne(OutboxDelivery delivery, CancellationToken cancellationToken)
        {
            var publisher = _publishers.FirstOrDefault(p => p.ConsumerName == delivery.ConsumerName);
            var result = publisher is null
                ? EventPublishResult.Failed($"no IEventPublisher registered for consumer '{delivery.ConsumerName}'.")
                : await publisher.PublishAsync(delivery.OutboxMessage.EventType, delivery.OutboxMessage.PayloadJson, cancellationToken);

            if (result.Success)
            {
                delivery.Status = OutboxStatus.Sent;
                delivery.ProcessedAtUtc = DateTime.UtcNow;
            }
            else
            {
                delivery.Attempts += 1;
                delivery.LastError = result.Error;

                if (delivery.Attempts >= MaxAttempts)
                {
                    // Unlike POS's Sale.StockSyncStatus, there's nothing
                    // on the Warehouse side that needs flagging when a
                    // delivery is finally given up on — the stock change
                    // itself already happened and committed regardless;
                    // only Reporting's picture of it is stale. The
                    // dead-lettered OutboxDelivery row itself IS the
                    // record of that, queryable if anyone needs to notice.
                    delivery.Status = OutboxStatus.Failed;
                }
            }

            await _outboxRepository.UpdateDeliveryAsync(delivery);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
