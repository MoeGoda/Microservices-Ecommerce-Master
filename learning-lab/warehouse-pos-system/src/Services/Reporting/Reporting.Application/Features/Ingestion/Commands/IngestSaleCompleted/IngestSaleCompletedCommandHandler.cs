using MediatR;
using Reporting.Application.Contracts.Persistence;
using Reporting.Application.Models;
using Reporting.Domain.Entities;

namespace Reporting.Application.Features.Ingestion.Commands.IngestSaleCompleted
{
    public class IngestSaleCompletedCommandHandler : IRequestHandler<IngestSaleCompletedCommand, IngestResultDto>
    {
        private readonly ISaleRecordRepository _saleRecordRepository;
        private readonly ISaleLineRecordRepository _saleLineRecordRepository;
        private readonly IUnitOfWork _unitOfWork;

        public IngestSaleCompletedCommandHandler(
            ISaleRecordRepository saleRecordRepository,
            ISaleLineRecordRepository saleLineRecordRepository,
            IUnitOfWork unitOfWork)
        {
            _saleRecordRepository = saleRecordRepository;
            _saleLineRecordRepository = saleLineRecordRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<IngestResultDto> Handle(IngestSaleCompletedCommand request, CancellationToken cancellationToken)
        {
            // The inbox check: at-least-once delivery (POS's outbox, C3/D1)
            // means this can arrive more than once for the same sale — a
            // repeat delivery is a no-op, not a duplicate SaleRecord.
            if (await _saleRecordRepository.ExistsForSale(request.SaleId))
            {
                return new IngestResultDto { AlreadyProcessed = true };
            }

            await _saleRecordRepository.AddAsync(new SaleRecord
            {
                SaleId = request.SaleId,
                LocationId = request.LocationId,
                CashierUserId = request.CashierUserId,
                Total = request.Total,
                CompletedAtUtc = request.CompletedAtUtc,
                LineCount = request.Lines.Count,
            });

            foreach (var line in request.Lines)
            {
                await _saleLineRecordRepository.AddAsync(new SaleLineRecord
                {
                    SaleId = request.SaleId,
                    ItemId = line.ItemId,
                    Sku = line.Sku,
                    ItemName = line.ItemName,
                    UnitPrice = line.UnitPrice,
                    Quantity = line.Quantity,
                    LineTotal = line.LineTotal,
                });
            }

            // One transaction: the SaleRecord and every SaleLineRecord it
            // implies commit together or not at all — the same "the
            // whole sale, atomically" reasoning ApplySaleCommand (Warehouse,
            // C3) already applies to stock decrements.
            await _unitOfWork.SaveChangesAsync();

            return new IngestResultDto { AlreadyProcessed = false };
        }
    }
}
