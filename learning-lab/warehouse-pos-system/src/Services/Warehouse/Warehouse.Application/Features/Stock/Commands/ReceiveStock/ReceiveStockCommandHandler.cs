using Common.Exceptions;
using MediatR;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Features.Stock;
using Warehouse.Application.Models;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Features.Stock.Commands.ReceiveStock
{
    public class ReceiveStockCommandHandler : IRequestHandler<ReceiveStockCommand, StockLevelDto>
    {
        private readonly IItemRepository _itemRepository;
        private readonly IItemUnitRepository _itemUnitRepository;
        private readonly IUnitOfMeasureRepository _unitOfMeasureRepository;
        private readonly StockAdjustmentStager _stager;
        private readonly IUnitOfWork _unitOfWork;

        public ReceiveStockCommandHandler(
            IItemRepository itemRepository,
            IItemUnitRepository itemUnitRepository,
            IUnitOfMeasureRepository unitOfMeasureRepository,
            StockAdjustmentStager stager,
            IUnitOfWork unitOfWork)
        {
            _itemRepository = itemRepository;
            _itemUnitRepository = itemUnitRepository;
            _unitOfMeasureRepository = unitOfMeasureRepository;
            _stager = stager;
            _unitOfWork = unitOfWork;
        }

        public async Task<StockLevelDto> Handle(ReceiveStockCommand request, CancellationToken cancellationToken)
        {
            var item = await _itemRepository.GetById(request.ItemId)
                ?? throw new NotFoundException(nameof(Item), request.ItemId);

            var unitOfMeasure = await _unitOfMeasureRepository.GetById(request.UnitOfMeasureId)
                ?? throw new NotFoundException(nameof(UnitOfMeasure), request.UnitOfMeasureId);

            var baseQuantity = await ConvertToBaseUnit(item, unitOfMeasure, request.Quantity);

            // Routed through the SAME staging path AdjustStockCommand/
            // ApplySaleCommand already use — createIfMissing: true is the
            // one difference a purchase-order receipt actually needs (it
            // can be the first stock this item has ever had at this
            // location). This closes the gap D1/D2/E1 each flagged in
            // turn: received stock now emits StockLevelChanged too, so
            // Reporting's read model and Notifications both hear about a
            // receipt, not just Warehouse's own StockLevel table.
            var staged = await _stager.Stage(item.Id, request.LocationId, baseQuantity, StockTransactionReason.Received, request.Reference, createIfMissing: true);

            // The StockLevel/StockTransaction change and the outbox event
            // Stage() staged all commit in the same call — B1's invariant
            // (summing every StockTransaction for an item+location always
            // equals its StockLevel) never has a window where it can be
            // violated, and Reporting/Notifications never hear about a
            // receipt that itself never actually committed.
            await _unitOfWork.SaveChangesAsync();

            return StockLevelDto.FromEntity(staged.StockLevel, staged.Item.BaseUnitOfMeasure.Code);
        }

        // Resolves how many of the item's BASE unit `quantity` of
        // `unitOfMeasure` represents. 1:1 if it's already the base unit;
        // otherwise looks up the item's ItemUnit conversion and rejects a
        // result that isn't a whole number — StockLevel.QuantityOnHand is
        // an int, so a fractional result (a bad conversion factor, or a
        // unit that genuinely doesn't divide evenly) is a data problem to
        // reject loudly, not round away silently.
        private async Task<int> ConvertToBaseUnit(Item item, UnitOfMeasure unitOfMeasure, decimal quantity)
        {
            if (unitOfMeasure.Id == item.BaseUnitOfMeasureId)
            {
                return (int)quantity;
            }

            var itemUnit = await _itemUnitRepository.GetByItemAndUnit(item.Id, unitOfMeasure.Id)
                ?? throw new NotFoundException(nameof(ItemUnit), $"item {item.Id}, unit {unitOfMeasure.Id}");

            var rawBaseQuantity = quantity * itemUnit.ConversionFactor;
            if (rawBaseQuantity != Math.Floor(rawBaseQuantity))
            {
                throw new ConflictException(
                    $"Converting {quantity} '{unitOfMeasure.Code}' of '{item.Sku}' to its base unit " +
                    $"({item.BaseUnitOfMeasure.Code}) yields {rawBaseQuantity}, not a whole number — check the conversion factor.");
            }

            return (int)rawBaseQuantity;
        }
    }
}
