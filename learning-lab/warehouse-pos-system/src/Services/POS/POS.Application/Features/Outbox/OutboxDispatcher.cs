using System.Text.Json;
using POS.Application.Contracts.Infrastructure;
using POS.Application.Contracts.Persistence;
using POS.Domain.Entities;

namespace POS.Application.Features.Outbox
{
    // The orchestration half of the outbox pattern, generalized from C3's
    // SaleCompletedOutboxDispatcher: "for every undelivered delivery,
    // find the publisher for its ConsumerName, try to deliver it, and
    // record what happened" — per DELIVERY now, not per event, so
    // Warehouse and Reporting each retry/succeed/fail independently for
    // the very same underlying event.
    public class OutboxDispatcher
    {
        // Bounded retries, then dead-letter — unchanged from C3, just
        // applied per delivery instead of per (necessarily single-consumer)
        // entry.
        private const int MaxAttempts = 5;

        private readonly IOutboxRepository _outboxRepository;
        private readonly ISaleRepository _saleRepository;
        private readonly IEnumerable<IEventPublisher> _publishers;
        private readonly IUnitOfWork _unitOfWork;

        public OutboxDispatcher(
            IOutboxRepository outboxRepository,
            ISaleRepository saleRepository,
            IEnumerable<IEventPublisher> publishers,
            IUnitOfWork unitOfWork)
        {
            _outboxRepository = outboxRepository;
            _saleRepository = saleRepository;
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
            if (publisher is null)
            {
                // A wiring/config problem (no IEventPublisher registered
                // for this ConsumerName), not a transient failure — still
                // goes through the same retry-then-dead-letter path so
                // it's visible in the outbox rather than silently stuck.
                await RecordOutcome(delivery, EventPublishResult.Failed($"no IEventPublisher registered for consumer '{delivery.ConsumerName}'."));
                return;
            }

            var result = await publisher.PublishAsync(delivery.OutboxMessage.EventType, delivery.OutboxMessage.PayloadJson, cancellationToken);
            await RecordOutcome(delivery, result);
        }

        private async Task RecordOutcome(OutboxDelivery delivery, EventPublishResult result)
        {
            if (result.Success)
            {
                delivery.Status = OutboxStatus.Sent;
                delivery.ProcessedAtUtc = DateTime.UtcNow;

                // The one piece of legacy, consumer-specific behavior this
                // otherwise-generic dispatcher still carries: Sale.StockSyncStatus
                // (C3) is specifically about whether WAREHOUSE confirmed the
                // stock decrement, not a generic "did any consumer get this"
                // flag — Reporting failing to ingest a sale doesn't mean the
                // stock never synced. Narrowly gated rather than generalized
                // away, since nothing else needs a per-consumer side effect
                // on Sale (yet).
                if (delivery.ConsumerName == OutboxConsumers.Warehouse && delivery.OutboxMessage.EventType == OutboxEventTypes.SaleCompleted)
                {
                    await MarkSaleStockSync(delivery, StockSyncStatus.Synced);
                }
            }
            else
            {
                delivery.Attempts += 1;
                delivery.LastError = result.Error;

                if (delivery.Attempts >= MaxAttempts)
                {
                    delivery.Status = OutboxStatus.Failed;

                    if (delivery.ConsumerName == OutboxConsumers.Warehouse && delivery.OutboxMessage.EventType == OutboxEventTypes.SaleCompleted)
                    {
                        // The compensating signal: this sale is Completed —
                        // the money was taken, checkout isn't reversed
                        // automatically — but Warehouse never confirmed the
                        // stock decrement. Someone has to reconcile this by
                        // hand.
                        await MarkSaleStockSync(delivery, StockSyncStatus.Failed);
                    }
                }
                // Otherwise it stays Pending — GetPendingDeliveries() will
                // pick it up again on the next dispatch cycle.
            }

            await _outboxRepository.UpdateDeliveryAsync(delivery);
            await _unitOfWork.SaveChangesAsync();
        }

        private async Task MarkSaleStockSync(OutboxDelivery delivery, StockSyncStatus status)
        {
            var message = JsonSerializer.Deserialize<SaleCompletedMessage>(delivery.OutboxMessage.PayloadJson);
            if (message is null)
            {
                return;
            }

            var sale = await _saleRepository.GetById(message.SaleId);
            if (sale is not null)
            {
                sale.StockSyncStatus = status;
                await _saleRepository.UpdateAsync(sale);
            }
        }
    }
}
