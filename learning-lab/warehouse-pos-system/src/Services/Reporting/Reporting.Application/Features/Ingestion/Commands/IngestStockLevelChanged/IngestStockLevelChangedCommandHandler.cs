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
                    Sku = request.Sku,
                    ItemName = request.ItemName,
                    LocationId = request.LocationId,
                    LocationCode = request.LocationCode,
                    LocationName = request.LocationName,
                    QuantityOnHand = request.QuantityOnHand,
                    ReorderThreshold = request.ReorderThreshold,
                    AsOfUtc = DateTime.UtcNow,
                };
                await _stockLevelRecordRepository.AddAsync(record);
            }
            else
            {
                // Sku/ItemName/LocationCode/LocationName rarely change,
                // but re-snapshotting them on every event costs nothing
                // and means a rename in Warehouse eventually catches up
                // here too, rather than freezing whatever the FIRST event
                // happened to say forever.
                record.Sku = request.Sku;
                record.ItemName = request.ItemName;
                record.LocationCode = request.LocationCode;
                record.LocationName = request.LocationName;
                record.QuantityOnHand = request.QuantityOnHand;
                record.ReorderThreshold = request.ReorderThreshold;
                record.AsOfUtc = DateTime.UtcNow;
                await _stockLevelRecordRepository.UpdateAsync(record);
            }

            await _unitOfWork.SaveChangesAsync();

            return new IngestResultDto { AlreadyProcessed = false };
        }
    }
}
