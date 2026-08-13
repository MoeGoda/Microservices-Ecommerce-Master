using MediatR;
using Reporting.Application.Contracts.Persistence;
using Reporting.Application.Models;
using Reporting.Domain.Entities;

namespace Reporting.Application.Features.Ingestion.Commands.IngestStockLevelChanged
{
    public class IngestStockLevelChangedCommandHandler : IRequestHandler<IngestStockLevelChangedCommand, IngestResultDto>
    {
        private readonly IStockLevelRecordRepository _stockLevelRecordRepository;
        private readonly IUnitOfWork _unitOfWork;

        public IngestStockLevelChangedCommandHandler(IStockLevelRecordRepository stockLevelRecordRepository, IUnitOfWork unitOfWork)
        {
            _stockLevelRecordRepository = stockLevelRecordRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<IngestResultDto> Handle(IngestStockLevelChangedCommand request, CancellationToken cancellationToken)
        {
            // Upsert, not insert-if-missing-else-reject — naturally
            // idempotent (see StockLevelRecord's own comment), so there's
            // no AlreadyProcessed branch here the way SaleCompleted needed
            // one; every delivery, first or repeated, ends up applying the
            // same result.
            var record = await _stockLevelRecordRepository.GetByItemAndLocation(request.ItemId, request.LocationId);
            if (record is null)
            {
                record = new StockLevelRecord
                {
                    ItemId = request.ItemId,
                    LocationId = request.LocationId,
                    QuantityOnHand = request.QuantityOnHand,
                    AsOfUtc = DateTime.UtcNow,
                };
                await _stockLevelRecordRepository.AddAsync(record);
            }
            else
            {
                record.QuantityOnHand = request.QuantityOnHand;
                record.AsOfUtc = DateTime.UtcNow;
                await _stockLevelRecordRepository.UpdateAsync(record);
            }

            await _unitOfWork.SaveChangesAsync();

            return new IngestResultDto { AlreadyProcessed = false };
        }
    }
}
