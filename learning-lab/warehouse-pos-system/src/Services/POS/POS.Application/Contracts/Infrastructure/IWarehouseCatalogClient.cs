namespace POS.Application.Contracts.Infrastructure
{
    // Same reasoning as Identity.Application's IJwtTokenGenerator/IPasswordHasher
    // (A1): a contract for something Infrastructure implements that isn't
    // persistence. AddSaleLineCommandHandler depends on this interface only —
    // it has no idea Warehouse.API exists, no idea the real implementation
    // makes an HTTP call at all. That's what makes this the actual seam C1's
    // README predicted: "a real Warehouse lookup gets inserted in front of
    // this call" without AddSaleLineCommand itself needing to change how it
    // thinks about where item data comes from.
    public interface IWarehouseCatalogClient
    {
        // Null if the barcode doesn't resolve to any item in Warehouse's
        // catalog — an ordinary "unknown barcode" outcome, not something
        // this call throws for. The caller (AddSaleLineCommandHandler)
        // decides what an unresolved scan means for a sale.
        Task<WarehouseItemLookup?> ResolveBarcodeAsync(string barcode, CancellationToken cancellationToken);

        // How many of this item are on hand at this specific location —
        // 0 if Warehouse has no stock record for that item+location pair
        // at all, same as it would be if the count had genuinely reached
        // zero. Never throws for "no stock" — that's an expected answer,
        // not a failure.
        Task<int> GetAvailableQuantityAsync(int itemId, int locationId, CancellationToken cancellationToken);
    }

    // Just enough of Warehouse's ItemDetailDto (B2) for a sale line to
    // snapshot — see SaleLine.cs for why Sku/ItemName/UnitPrice get copied
    // rather than kept as a live reference.
    public class WarehouseItemLookup
    {
        public int ItemId { get; set; }
        public string Sku { get; set; } = null!;
        public string ItemName { get; set; } = null!;

        // Already the price to charge — if Warehouse has an active
        // Promotion (C5) for this item, UnitPrice here is the DISCOUNTED
        // price; OriginalUnitPrice/PromotionId are only set in that case,
        // for AddSaleLineCommandHandler to snapshot onto the SaleLine for
        // receipt transparency. POS never computes a discount itself —
        // Warehouse is the one place that knows about promotions at all.
        public decimal UnitPrice { get; set; }
        public decimal? OriginalUnitPrice { get; set; }
        public int? PromotionId { get; set; }
    }
}
