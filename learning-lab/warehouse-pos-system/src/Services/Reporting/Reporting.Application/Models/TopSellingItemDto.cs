namespace Reporting.Application.Models
{
    // Aggregated across every SaleLineRecord ever ingested for this
    // ItemId — Sku/ItemName come from whichever line happened to be read
    // first in the grouping (they're stable across a real item's sales,
    // barring a rename mid-report, the same tolerance StockLevelRecord's
    // own denormalized fields accept).
    public class TopSellingItemDto
    {
        public int ItemId { get; set; }
        public string Sku { get; set; } = null!;
        public string ItemName { get; set; } = null!;
        public int TotalQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
