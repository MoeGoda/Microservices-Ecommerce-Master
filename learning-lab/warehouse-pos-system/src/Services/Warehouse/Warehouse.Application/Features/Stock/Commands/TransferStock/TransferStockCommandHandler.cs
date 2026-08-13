using MediatR;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Models;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Features.Stock.Commands.TransferStock
{
    public class TransferStockCommandHandler : IRequestHandler<TransferStockCommand, TransferStockResultDto>
    {
        private readonly StockAdjustmentStager _stager;
        private readonly IUnitOfWork _unitOfWork;

        public TransferStockCommandHandler(StockAdjustmentStager stager, IUnitOfWork unitOfWork)
        {
            _stager = stager;
            _unitOfWork = unitOfWork;
        }

        public async Task<TransferStockResultDto> Handle(TransferStockCommand request, CancellationToken cancellationToken)
        {
            // Source first: if the source doesn't have enough stock,
            // Stage() throws InsufficientStockException here, before the
            // destination is ever touched. Neither Stage() call commits by
            // itself (see StockAdjustmentStager's own comment) — only the
            // SaveChangesAsync below does — so a failure here leaves the
            // database exactly as it was: no half-transfer where stock
            // vanished from the source without appearing at the
            // destination.
            var from = await _stager.Stage(
                request.ItemId,
                request.FromLocationId,
                -request.Quantity,
                StockTransactionReason.TransferOut,
                request.Reference);

            // createIfMissing: true — this can legitimately be the first
            // stock this item has ever had at the destination location,
            // the same reasoning ReceiveStockCommandHandler needed it for.
            var to = await _stager.Stage(
                request.ItemId,
                request.ToLocationId,
                request.Quantity,
                StockTransactionReason.TransferIn,
                request.Reference,
                createIfMissing: true);

            await _unitOfWork.SaveChangesAsync();

            return new TransferStockResultDto
            {
                From = StockLevelDto.FromEntity(from.StockLevel, from.Item.BaseUnitOfMeasure.Code),
                To = StockLevelDto.FromEntity(to.StockLevel, to.Item.BaseUnitOfMeasure.Code),
            };
        }
    }
}
