using System.Text.Json;
using POS.Application.Contracts.Infrastructure;
using POS.Application.Contracts.Persistence;
using POS.Domain.Entities;

namespace POS.Application.Features.Outbox
{
    // The orchestration half of the outbox pattern: "for every
    // undelivered SaleCompleted event, try to deliver it, and record
    // what happened" — deliberately not a MediatR command, since nothing
    // external ever asks for this; it's driven by a poll loop (see
    // SaleCompletedOutboxBackgroundService, POS.Infrastructure). Kept in
    // the Application layer, not Infrastructure, because deciding the
    // retry policy and what "give up" means is a business decision the
    // same way deciding when a StockLevel needs creating vs. updating was
    // (Warehouse B2) — not a mechanical detail belonging to whatever
    // transport actually sends the HTTP request.
    public class SaleCompletedOutboxDispatcher
    {
        // Bounded retries, then dead-letter. A real system might classify
        // failures more precisely (a network blip is worth retrying
        // longer than a definitive rejection) — this keeps one uniform
        // policy rather than guessing at that distinction prematurely.
        private const int MaxAttempts = 5;

        private readonly ISaleCompletedOutboxRepository _outboxRepository;
        private readonly ISaleRepository _saleRepository;
        private readonly ISaleCompletedPublisher _publisher;
        private readonly IUnitOfWork _unitOfWork;

        public SaleCompletedOutboxDispatcher(
            ISaleCompletedOutboxRepository outboxRepository,
            ISaleRepository saleRepository,
            ISaleCompletedPublisher publisher,
            IUnitOfWork unitOfWork)
        {
            _outboxRepository = outboxRepository;
            _saleRepository = saleRepository;
            _publisher = publisher;
            _unitOfWork = unitOfWork;
        }

        public async Task DispatchPendingAsync(CancellationToken cancellationToken)
        {
            var entries = await _outboxRepository.GetPending();
            foreach (var entry in entries)
            {
                await DispatchOne(entry, cancellationToken);
            }
        }

        private async Task DispatchOne(SaleCompletedOutboxEntry entry, CancellationToken cancellationToken)
        {
            var lines = JsonSerializer.Deserialize<List<SaleCompletedLine>>(entry.LinesJson) ?? new List<SaleCompletedLine>();
            var message = new SaleCompletedMessage
            {
                SaleId = entry.SaleId,
                LocationId = entry.LocationId,
                Lines = lines,
            };

            var result = await _publisher.PublishAsync(message, cancellationToken);
            var sale = await _saleRepository.GetById(entry.SaleId);

            if (result.Success)
            {
                entry.Status = OutboxStatus.Sent;
                entry.ProcessedAtUtc = DateTime.UtcNow;

                if (sale is not null)
                {
                    sale.StockSyncStatus = StockSyncStatus.Synced;
                    await _saleRepository.UpdateAsync(sale);
                }
            }
            else
            {
                entry.Attempts += 1;
                entry.LastError = result.Error;

                if (entry.Attempts >= MaxAttempts)
                {
                    // The compensating signal: this sale is Completed —
                    // the money was taken, checkout isn't reversed
                    // automatically — but Warehouse never actually
                    // confirmed the stock decrement. Someone has to
                    // reconcile this by hand.
                    entry.Status = OutboxStatus.Failed;

                    if (sale is not null)
                    {
                        sale.StockSyncStatus = StockSyncStatus.Failed;
                        await _saleRepository.UpdateAsync(sale);
                    }
                }
                // Otherwise it stays Pending — GetPending() will pick it
                // up again on the next dispatch cycle.
            }

            await _outboxRepository.UpdateAsync(entry);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
