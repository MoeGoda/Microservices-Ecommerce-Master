namespace Warehouse.Application.Models
{
    // J — one row per PurchaseOrder, every status included so the
    // Angular report can show a status breakdown (counts per status) as
    // well as age — not just the open ones. AgeDaysSinceOrdered is only
    // meaningful once an order has actually been submitted (Ordered or
    // later); it's null for Draft (never submitted) and for
    // Received/Cancelled (nothing left to wait on).
    public class PurchaseOrderAgingLineDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = null!;
        public string SupplierName { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime? OrderedAtUtc { get; set; }
        public int? AgeDaysSinceOrdered { get; set; }
        public decimal TotalCost { get; set; }
        public decimal ReceivedValue { get; set; }
    }
}
