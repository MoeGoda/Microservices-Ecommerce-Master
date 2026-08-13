using System.Text.Json;
using Common.Exceptions;
using MediatR;
using POS.Application.Contracts.Infrastructure;
using POS.Application.Contracts.Persistence;
using POS.Application.Features.Outbox;
using POS.Application.Models;
using POS.Domain.Entities;

namespace POS.Application.Features.Sales.Commands.Checkout
{
    public class CheckoutCommandHandler : IRequestHandler<CheckoutCommand, SaleDto>
    {
        // Every consumer a SaleCompleted event fans out to today —
        // Warehouse (decrement stock, C3) and Reporting (project a
        // SaleRecord/SaleLineRecord read model, D1). Adding a THIRD
        // consumer later (E1's notifications, most likely) means adding
        // one more name here and one more IEventPublisher implementation —
        // nothing about the outbox/dispatcher machinery itself changes.
        private static readonly string[] SaleCompletedConsumers = { OutboxConsumers.Warehouse, OutboxConsumers.Reporting };

        private readonly ISaleRepository _saleRepository;
        private readonly ISaleLineRepository _saleLineRepository;
        private readonly IOutboxRepository _outboxRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CheckoutCommandHandler(
            ISaleRepository saleRepository,
            ISaleLineRepository saleLineRepository,
            IOutboxRepository outboxRepository,
            IUnitOfWork unitOfWork)
        {
            _saleRepository = saleRepository;
            _saleLineRepository = saleLineRepository;
            _outboxRepository = outboxRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<SaleDto> Handle(CheckoutCommand request, CancellationToken cancellationToken)
        {
            var sale = await _saleRepository.GetById(request.SaleId)
                ?? throw new NotFoundException(nameof(Sale), request.SaleId);

            if (sale.Status != SaleStatus.InProgress)
            {
                throw new ConflictException($"Sale {sale.Id} is {sale.Status}; only an InProgress sale can be checked out.");
            }

            var lines = (await _saleLineRepository.GetBySale(sale.Id)).ToList();
            if (lines.Count == 0)
            {
                throw new ConflictException($"Sale {sale.Id} has no lines; add at least one before checking out.");
            }

            sale.Status = SaleStatus.Completed;
            sale.CompletedAt = DateTime.UtcNow;
            sale.StockSyncStatus = StockSyncStatus.Pending;
            await _saleRepository.UpdateAsync(sale);

            // The Outbox pattern: the message, every delivery it fans out
            // to, and the Sale's own Status change above all commit in
            // the SAME SaveChanges call below — "the sale completed" and
            // "an event was queued for every interested consumer" either
            // all happen or none do. Writing straight to an HTTP call
            // here instead (skipping this table) would reintroduce
            // exactly the failure window this avoids: a crash between
            // "commit the sale" and "send the request" would complete the
            // sale with nobody ever told. A background dispatcher
            // (OutboxDispatcher, not part of this request) picks these
            // rows up separately — checkout returns as soon as POS's own
            // commit succeeds; it does not wait for Warehouse or
            // Reporting to actually apply anything.
            //
            // Serialized as the SAME SaleCompletedMessage/SaleCompletedLine
            // types OutboxDispatcher's publishers deserialize back into —
            // not an ad-hoc anonymous type — so the round trip can't
            // silently drift out of sync on property casing between the
            // write side and the read side (see C3 for the real bug this
            // exact discipline was added to prevent).
            var message = new SaleCompletedMessage
            {
                SaleId = sale.Id,
                LocationId = sale.LocationId,
                CashierUserId = sale.CashierUserId,
                Total = sale.Total,
                CompletedAtUtc = sale.CompletedAt!.Value,
                Lines = lines.Select(l => new SaleCompletedLine
                {
                    ItemId = l.ItemId,
                    Sku = l.Sku,
                    ItemName = l.ItemName,
                    UnitPrice = l.UnitPrice,
                    Quantity = l.Quantity,
                    LineTotal = l.LineTotal,
                }).ToList(),
            };

            var outboxMessage = await _outboxRepository.AddMessageAsync(new OutboxMessage
            {
                EventType = OutboxEventTypes.SaleCompleted,
                PayloadJson = JsonSerializer.Serialize(message),
            });

            foreach (var consumer in SaleCompletedConsumers)
            {
                await _outboxRepository.AddDeliveryAsync(new OutboxDelivery
                {
                    OutboxMessage = outboxMessage,
                    ConsumerName = consumer,
                });
            }

            await _unitOfWork.SaveChangesAsync();

            return SaleDto.FromEntity(sale, lines);
        }
    }
}
