using Common.Exceptions;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Exceptions;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Features.Stock
{
    // Extracted out of AdjustStockCommandHandler the moment a SECOND
    // caller (ApplySaleCommandHandler, C3) needed the identical
    // "fetch the StockLevel, guard against going negative, write the
    // matching StockTransaction" logic — the same reasoning as
    // Common.Security.JwtTokenFactory getting pulled out in C2. The
    // deliberate design point: this method only ever STAGES a change; it
    // never calls SaveChangesAsync. AdjustStockCommandHandler commits
    // after exactly one call — one line, one adjustment. ApplySaleCommandHandler
    // calls it once PER LINE of a sale and commits ONCE at the end, which
    // is what makes a multi-line sale's stock decrement atomic: if line 3
    // of 4 would go negative, lines 1-2's staged changes are simply never
    // saved. No hand-rolled compensating rollback needed for that case —
    // it's one database, so a normal transaction boundary (deferring
    // SaveChanges) does the job a saga would otherwise have to do by hand.
    public class StockAdjustmentStager
    {
        private readonly IItemRepository _itemRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly IStockLevelRepository _stockLevelRepository;
        private readonly IStockTransactionRepository _stockTransactionRepository;

        public StockAdjustmentStager(
            IItemRepository itemRepository,
            ILocationRepository locationRepository,
            IStockLevelRepository stockLevelRepository,
            IStockTransactionRepository stockTransactionRepository)
        {
            _itemRepository = itemRepository;
            _locationRepository = locationRepository;
            _stockLevelRepository = stockLevelRepository;
            _stockTransactionRepository = stockTransactionRepository;
        }

        public async Task<StagedAdjustment> Stage(int itemId, int locationId, int quantityChange, StockTransactionReason reason, string? reference)
        {
            var item = await _itemRepository.GetById(itemId)
                ?? throw new NotFoundException(nameof(Item), itemId);

            var location = await _locationRepository.GetById(locationId)
                ?? throw new NotFoundException(nameof(Location), locationId);

            var stockLevel = await _stockLevelRepository.GetByItemAndLocation(item.Id, location.Id)
                ?? throw new NotFoundException(nameof(StockLevel), $"item {item.Id}, location {location.Id}");

            var newQuantity = stockLevel.QuantityOnHand + quantityChange;
            if (newQuantity < 0)
            {
                throw new InsufficientStockException(item.Name, location.Name, stockLevel.QuantityOnHand, quantityChange);
            }

            stockLevel.QuantityOnHand = newQuantity;
            await _stockLevelRepository.UpdateAsync(stockLevel);

            var transaction = new StockTransaction
            {
                ItemId = item.Id,
                LocationId = location.Id,
                QuantityChange = quantityChange,
                Reason = reason,
                Reference = reference,
            };
            await _stockTransactionRepository.AddAsync(transaction);

            return new StagedAdjustment { StockLevel = stockLevel, Item = item };
        }
    }

    // Handed back so a caller that needs to build a StockLevelDto (its
    // BaseUnitOfMeasure.Code) doesn't have to re-fetch the Item a second time.
    public class StagedAdjustment
    {
        public StockLevel StockLevel { get; set; } = null!;
        public Item Item { get; set; } = null!;
    }
}
