namespace Warehouse.Application.Models
{
    // J — one row per Item, summed ACROSS every location — "how much of
    // this do we have anywhere, and what's it worth" is the question
    // this report answers, not a per-location breakdown (StockLevelDto
    // already answers that one). Valued at the item's current UnitPrice
    // (the selling price), not a purchase cost — Warehouse has no
    // per-item "standard cost" field anywhere; PurchaseOrderLine.UnitCost
    // is a fact about one specific order, not a running attribute of the
    // Item itself, so it isn't a sound stand-in for one. A named,
    // deliberate scope cut: "value at today's selling price," not "cost
    // of goods on hand."
    public class InventoryValuationLineDto
    {
        public int ItemId { get; set; }
        public string Sku { get; set; } = null!;
        public string ItemName { get; set; } = null!;
        public string CategoryName { get; set; } = null!;
        public int TotalQuantityOnHand { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalValue { get; set; }
    }
}
