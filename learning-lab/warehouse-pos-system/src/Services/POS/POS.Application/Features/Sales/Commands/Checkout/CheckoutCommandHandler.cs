using System.Text.Json;
using Common.Exceptions;
using MediatR;
using POS.Application.Contracts.Infrastructure;
using POS.Application.Contracts.Persistence;
using POS.Application.Models;
using POS.Domain.Entities;

namespace POS.Application.Features.Sales.Commands.Checkout
{
    public class CheckoutCommandHandler : IRequestHandler<CheckoutCommand, SaleDto>
    {
        private readonly ISaleRepository _saleRepository;
        private readonly ISaleLineRepository _saleLineRepository;
        private readonly ISaleCompletedOutboxRepository _outboxRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CheckoutCommandHandler(
            ISaleRepository saleRepository,
            ISaleLineRepository saleLineRepository,
            ISaleCompletedOutboxRepository outboxRepository,
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

            // The Outbox pattern: this row and the Sale's own Status
            // change above commit in the SAME SaveChanges call below —
            // "the sale completed" and "an event was queued to tell
            // Warehouse" either both happen or neither does. Writing the
            // event straight to an HTTP call here instead (skipping this
            // table) would reintroduce exactly the failure window this
            // avoids: a crash between "commit the sale" and "send the
            // request" would complete the sale with Warehouse never told.
            // A background dispatcher (SaleCompletedOutboxDispatcher, not
            // part of this request) picks this row up separately —
            // checkout returns as soon as POS's own commit succeeds; it
            // does not wait for Warehouse to actually apply anything.
            // Serialized as the SAME SaleCompletedLine type
            // SaleCompletedOutboxDispatcher deserializes it back into —
            // not an ad-hoc anonymous type — so the round trip can't
            // silently drift out of sync on property casing between the
            // write side and the read side.
            var linesJson = JsonSerializer.Serialize(lines.Select(l => new SaleCompletedLine { ItemId = l.ItemId, Quantity = l.Quantity }).ToList());
            await _outboxRepository.AddAsync(new SaleCompletedOutboxEntry
            {
                SaleId = sale.Id,
                LocationId = sale.LocationId,
                LinesJson = linesJson,
            });

            await _unitOfWork.SaveChangesAsync();

            return SaleDto.FromEntity(sale, lines);
        }
    }
}
