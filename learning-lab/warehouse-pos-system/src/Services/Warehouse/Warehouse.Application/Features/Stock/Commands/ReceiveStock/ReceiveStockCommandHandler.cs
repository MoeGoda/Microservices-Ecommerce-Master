using Common.Exceptions;
using MediatR;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Models;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Features.Stock.Commands.ReceiveStock
{
    public class ReceiveStockCommandHandler : IRequestHandler<ReceiveStockCommand, StockLevelDto>
    {
        private readonly IItemRepository _itemRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly IItemUnitRepository _itemUnitRepository;
        private readonly IUnitOfMeasureRepository _unitOfMeasureRepository;
        private readonly IStockLevelRepository _stockLevelRepository;
        private readonly IStockTransactionRepository _stockTransactionRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ReceiveStockCommandHandler(
            IItemRepository itemRepository,
            ILocationRepository locationRepository,
            IItemUnitRepository itemUnitRepository,
            IUnitOfMeasureRepository unitOfMeasureRepository,
            IStockLevelRepository stockLevelRepository,
            IStockTransactionRepository stockTransactionRepository,
            IUnitOfWork unitOfWork)
        {
            _itemRepository = itemRepository;
            _locationRepository = locationRepository;
            _itemUnitRepository = itemUnitRepository;
            _unitOfMeasureRepository = unitOfMeasureRepository;
            _stockLevelRepository = stockLevelRepository;
            _stockTransactionRepository = stockTransactionRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<StockLevelDto> Handle(ReceiveStockCommand request, CancellationToken cancellationToken)
        {
            var item = await _itemRepository.GetById(request.ItemId)
                ?? throw new NotFoundException(nameof(Item), request.ItemId);

            var location = await _locationRepository.GetById(request.LocationId)
                ?? throw new NotFoundException(nameof(Location), request.LocationId);

            var unitOfMeasure = await _unitOfMeasureRepository.GetById(request.UnitOfMeasureId)
                ?? throw new NotFoundException(nameof(UnitOfMeasure), request.UnitOfMeasureId);

            var baseQuantity = await ConvertToBaseUnit(item, unitOfMeasure, request.Quantity);

            var stockLevel = await _stockLevelRepository.GetByItemAndLocation(item.Id, location.Id);
            if (stockLevel is null)
            {
                stockLevel = new StockLevel
                {
                    ItemId = item.Id,
                    LocationId = location.Id,
                    Location = location,
                    QuantityOnHand = baseQuantity,
                    ReorderThreshold = 0,
                    UnitOfMeasureId = item.BaseUnitOfMeasureId,
                    UnitOfMeasure = item.BaseUnitOfMeasure,
                };
                await _stockLevelRepository.AddAsync(stockLevel);
            }
            else
            {
                stockLevel.QuantityOnHand += baseQuantity;
                await _stockLevelRepository.UpdateAsync(stockLevel);
            }

            var transaction = new StockTransaction
            {
                ItemId = item.Id,
                LocationId = location.Id,
                QuantityChange = baseQuantity,
                Reason = StockTransactionReason.Received,
                Reference = request.Reference,
            };
            await _stockTransactionRepository.AddAsync(transaction);

            // The StockLevel change and the StockTransaction that explains
            // it commit in the same call — B1's invariant (summing every
            // StockTransaction for an item+location always equals its
            // StockLevel) never has a window where it can be violated.
            await _unitOfWork.SaveChangesAsync();

            return StockLevelDto.FromEntity(stockLevel, item.BaseUnitOfMeasure.Code);
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
