using MediatR;
using Reporting.Application.Contracts.Persistence;
using Reporting.Application.Models;
using Reporting.Domain.Entities;

namespace Reporting.Application.Features.Ingestion.Commands.IngestStockTransactionRecorded
{
    public class IngestStockTransactionRecordedCommandHandler : IRequestHandler<IngestStockTransactionRecordedCommand, IngestResultDto>
    {
        private readonly IStockMovementRecordRepository _stockMovementRecordRepository;
        private readonly IUnitOfWork _unitOfWork;

        public IngestStockTransactionRecordedCommandHandler(IStockMovementRecordRepository stockMovementRecordRepository, IUnitOfWork unitOfWork)
        {
            _stockMovementRecordRepository = stockMovementRecordRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<IngestResultDto> Handle(IngestStockTransactionRecordedCommand request, CancellationToken cancellationToken)
        {
            // Always a plain insert — see StockMovementRecord's own
            // comment on why there's no dedup check here the way
            // SaleRecord's ExistsForSale needed one.
            await _stockMovementRecordRepository.AddAsync(new StockMovementRecord
            {
                ItemId = request.ItemId,
                Sku = request.Sku,
                ItemName = request.ItemName,
                LocationId = request.LocationId,
                LocationCode = request.LocationCode,
                LocationName = request.LocationName,
                QuantityChange = request.QuantityChange,
                Reason = request.Reason,
                Reference = request.Reference,
                TransactionAtUtc = request.TransactionAtUtc,
            });

            await _unitOfWork.SaveChangesAsync();

            return new IngestResultDto { AlreadyProcessed = false };
        }
    }
}
