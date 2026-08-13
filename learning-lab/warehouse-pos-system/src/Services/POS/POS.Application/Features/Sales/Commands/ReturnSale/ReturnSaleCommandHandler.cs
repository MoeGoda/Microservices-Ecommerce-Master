using System.Text.Json;
using Common.Exceptions;
using MediatR;
using POS.Application.Contracts.Infrastructure;
using POS.Application.Contracts.Persistence;
using POS.Application.Features.Outbox;
using POS.Application.Models;
using POS.Domain.Entities;

namespace POS.Application.Features.Sales.Commands.ReturnSale
{
    public class ReturnSaleCommandHandler : IRequestHandler<ReturnSaleCommand, SaleDto>
    {
        // Same three consumers SaleCompleted already fans out to (E1's own
        // comment on CheckoutCommandHandler predicted exactly this: "adding
        // a THIRD consumer... nothing about the outbox/dispatcher machinery
        // itself changes" — the pattern extends just as cleanly to a
        // second event TYPE, not just a second consumer).
        private static readonly string[] SaleReturnedConsumers = { OutboxConsumers.Warehouse, OutboxConsumers.Reporting, OutboxConsumers.Notifications };

        private readonly ISaleRepository _saleRepository;
        private readonly ISaleLineRepository _saleLineRepository;
        private readonly IOutboxRepository _outboxRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ReturnSaleCommandHandler(
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

        public async Task<SaleDto> Handle(ReturnSaleCommand request, CancellationToken cancellationToken)
        {
            var sale = await _saleRepository.GetById(request.SaleId)
                ?? throw new NotFoundException(nameof(Sale), request.SaleId);

            if (sale.Status != SaleStatus.Completed)
            {
                throw new ConflictException($"Sale {sale.Id} is {sale.Status}; only a Completed sale can be returned.");
            }

            var lines = (await _saleLineRepository.GetBySale(sale.Id)).ToList();

            sale.Status = SaleStatus.Returned;
            sale.ReturnedAt = DateTime.UtcNow;
            await _saleRepository.UpdateAsync(sale);

            // Same SaleCompletedMessage shape the original completion used —
            // a return describes the exact same sale/lines, just with the
            // opposite meaning, which is carried entirely by EventType
            // rather than a second message shape.
            var message = new SaleCompletedMessage
            {
                SaleId = sale.Id,
                LocationId = sale.LocationId,
                CashierUserId = sale.CashierUserId,
                Total = sale.Total,
                CompletedAtUtc = sale.ReturnedAt!.Value,
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
                EventType = OutboxEventTypes.SaleReturned,
                PayloadJson = JsonSerializer.Serialize(message),
            });

            foreach (var consumer in SaleReturnedConsumers)
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
