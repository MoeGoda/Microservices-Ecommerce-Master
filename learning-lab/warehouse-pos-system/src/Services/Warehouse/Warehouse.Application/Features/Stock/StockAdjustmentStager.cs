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
    // D1 adds a StockLevelChanged outbox event here too, which means every
    // caller of Stage() emits it automatically. ReceiveStockCommandHandler
    // (a purchase-order receipt) went without this for D1/D2/E1 because it
    // never called Stage() at all — it needs unit conversion first
    // (ConvertToBaseUnit, which Stage() doesn't do), AND it can be the
    // FIRST stock this item has ever had at this location, which Stage()
    // didn't support (AdjustStockCommand/ApplySaleCommand both require a
    // balance to already exist — "adjusting" implies one does). The
    // createIfMissing parameter below is the one behavioral difference
    // ReceiveStockCommandHandler actually needs; everything else about
    // staging a change — the negative-balance guard, the StockTransaction,
    // the outbox event — is identical, so it's a parameter on the same
    // method rather than a separate one.
    public class StockAdjustmentStager
    {
        // Every consumer a StockLevelChanged event fans out to today —
        // Reporting (project a StockLevelRecord read model, D1/D2) and now
        // Notifications (a "Low stock" toast, E1) — the same array-of-
        // consumers idiom POS's own CheckoutCommandHandler already uses
        // for SaleCompleted's fan-out, adopted here now that this event
        // has more than one destination too.
        private static readonly string[] StockLevelChangedConsumers = { OutboxConsumers.Reporting, OutboxConsumers.Notifications };

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

        public async Task<StagedAdjustment> Stage(int itemId, int locationId, int quantityChange, StockTransactionReason reason, string? reference, bool createIfMissing = false)
        {
            var item = await _itemRepository.GetById(itemId)
                ?? throw new NotFoundException(nameof(Item), itemId);

            var location = await _locationRepository.GetById(locationId)
                ?? throw new NotFoundException(nameof(Location), locationId);

            var existingStockLevel = await _stockLevelRepository.GetByItemAndLocation(item.Id, location.Id);
            var isNewStockLevel = existingStockLevel is null;
            StockLevel stockLevel;
            if (existingStockLevel is null)
            {
                if (!createIfMissing)
                {
                    throw new NotFoundException(nameof(StockLevel), $"item {item.Id}, location {location.Id}");
                }

                // First-ever stock for this item at this location — a
                // fresh row, not an update. ReorderThreshold defaults to
                // 0 (never low) until someone sets a real one; that's the
                // same default ReceiveStockCommandHandler always used.
                stockLevel = new StockLevel
                {
                    ItemId = item.Id,
                    LocationId = location.Id,
                    Location = location,
                    QuantityOnHand = 0,
                    ReorderThreshold = 0,
                    UnitOfMeasureId = item.BaseUnitOfMeasureId,
                    UnitOfMeasure = item.BaseUnitOfMeasure,
                };
            }
            else
            {
                stockLevel = existingStockLevel;
            }

            var newQuantity = stockLevel.QuantityOnHand + quantityChange;
            if (newQuantity < 0)
            {
                throw new InsufficientStockException(item.Name, location.Name, stockLevel.QuantityOnHand, quantityChange);
            }

            stockLevel.QuantityOnHand = newQuantity;

            // AddAsync with the final quantity already set, rather than
            // AddAsync-then-UpdateAsync — the entity has no Id yet in the
            // new-row case, so there's nothing for a second Update() call
            // to usefully do that setting the property before the single
            // Add doesn't already cover.
            if (isNewStockLevel)
            {
                await _stockLevelRepository.AddAsync(stockLevel);
            }
            else
            {
                await _stockLevelRepository.UpdateAsync(stockLevel);
            }

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
            foreach (var consumer in StockLevelChangedConsumers)
            {
                await _outboxRepository.AddDeliveryAsync(new OutboxDelivery
                {
                    OutboxMessage = outboxMessage,
                    ConsumerName = consumer,
                });
            }

            // J — the same StockTransaction row just written above, fanned
            // out as its own event rather than folded into
            // StockLevelChanged: that message is a balance snapshot and
            // stays one, so a movement ledger needs a second, independent
            // event carrying the delta/reason/reference instead. Staged in
            // the SAME unit of work as everything above, for the identical
            // atomicity reason.
            var movementMessage = await _outboxRepository.AddMessageAsync(new OutboxMessage
            {
                EventType = OutboxEventTypes.StockTransactionRecorded,
                PayloadJson = JsonSerializer.Serialize(new StockTransactionRecordedMessage
                {
                    ItemId = item.Id,
                    Sku = item.Sku,
                    ItemName = item.Name,
                    LocationId = location.Id,
                    LocationCode = location.Code,
                    LocationName = location.Name,
                    QuantityChange = quantityChange,
                    Reason = reason.ToString(),
                    Reference = reference,
                    TransactionAtUtc = transaction.CreatedAt,
                }),
            });
            await _outboxRepository.AddDeliveryAsync(new OutboxDelivery
            {
                OutboxMessage = movementMessage,
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
