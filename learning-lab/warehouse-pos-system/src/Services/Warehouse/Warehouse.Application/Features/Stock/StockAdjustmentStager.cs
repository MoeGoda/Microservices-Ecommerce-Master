using System.Text.Json;
using Common.Exceptions;
using Warehouse.Application.Contracts.Infrastructure;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Exceptions;
using Warehouse.Application.Features.Outbox;
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
    //
    // D1 adds a StockLevelChanged outbox event here too, which means both
    // of this method's callers (AdjustStockCommand, ApplySaleCommand) now
    // emit it automatically — but ReceiveStockCommandHandler does NOT,
    // since it never calls Stage() (it needs unit conversion first,
    // ConvertToBaseUnit, that Stage() doesn't do). A real deployment would
    // want stock received via a PO to show up in Reporting too; that gap
    // is flagged here rather than closed, the same "narrow the fix to
    // what's actually wired up, name what isn't" discipline this codebase
    // has followed elsewhere.
    public class StockAdjustmentStager
    {
        private readonly IItemRepository _itemRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly IStockLevelRepository _stockLevelRepository;
        private readonly IStockTransactionRepository _stockTransactionRepository;
        private readonly IOutboxRepository _outboxRepository;

        public StockAdjustmentStager(
            IItemRepository itemRepository,
            ILocationRepository locationRepository,
            IStockLevelRepository stockLevelRepository,
            IStockTransactionRepository stockTransactionRepository,
            IOutboxRepository outboxRepository)
        {
            _itemRepository = itemRepository;
            _locationRepository = locationRepository;
            _stockLevelRepository = stockLevelRepository;
            _stockTransactionRepository = stockTransactionRepository;
            _outboxRepository = outboxRepository;
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

            // Staged in the SAME unsaved unit of work as the StockLevel/
            // StockTransaction change above (D1) — every caller of Stage()
            // gets this for free, the whole point of extracting it here
            // rather than duplicating it in AdjustStockCommandHandler and
            // ApplySaleCommandHandler separately. It also means the same
            // atomicity guarantee this method's own comment already
            // describes covers the event too: if a LATER line in a
            // multi-line ApplySaleCommand fails and the caller never
            // reaches SaveChangesAsync, this staged (but unsaved) event
            // for an EARLIER line is discarded right along with its
            // StockLevel/StockTransaction — Reporting never hears about a
            // stock change that itself never actually committed.
            // Sku/ItemName/LocationCode/LocationName/ReorderThreshold are
            // denormalized onto the event (D2) — Reporting has no other
            // way to know an item's name or a location's code; it only
            // ever hears about either through an event. Item/Location are
            // already loaded here, so this costs nothing extra to include.
            var outboxMessage = await _outboxRepository.AddMessageAsync(new OutboxMessage
            {
                EventType = OutboxEventTypes.StockLevelChanged,
                PayloadJson = JsonSerializer.Serialize(new StockLevelChangedMessage
                {
                    ItemId = item.Id,
                    Sku = item.Sku,
                    ItemName = item.Name,
                    LocationId = location.Id,
                    LocationCode = location.Code,
                    LocationName = location.Name,
                    QuantityOnHand = stockLevel.QuantityOnHand,
                    ReorderThreshold = stockLevel.ReorderThreshold,
                }),
            });
            await _outboxRepository.AddDeliveryAsync(new OutboxDelivery
            {
                OutboxMessage = outboxMessage,
                ConsumerName = OutboxConsumers.Reporting,
            });

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
