using MediatR;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Features.Stock;
using Warehouse.Application.Models;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Features.Stock.Commands.AdjustStock
{
    public class AdjustStockCommandHandler : IRequestHandler<AdjustStockCommand, StockLevelDto>
    {
        private readonly StockAdjustmentStager _stager;
        private readonly IUnitOfWork _unitOfWork;

        public AdjustStockCommandHandler(StockAdjustmentStager stager, IUnitOfWork unitOfWork)
        {
            _stager = stager;
            _unitOfWork = unitOfWork;
        }

        public async Task<StockLevelDto> Handle(AdjustStockCommand request, CancellationToken cancellationToken)
        {
            var staged = await _stager.Stage(
                request.ItemId,
                request.LocationId,
                request.QuantityChange,
                StockTransactionReason.Adjustment,
                request.Reference);

            await _unitOfWork.SaveChangesAsync();

            return StockLevelDto.FromEntity(staged.StockLevel, staged.Item.BaseUnitOfMeasure.Code);
        }
    }
}
