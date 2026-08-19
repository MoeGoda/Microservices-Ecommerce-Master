using POS.Domain.Common;

namespace POS.Domain.Entities
{
    // One cashier's shift at one register, from "opened" to "closed" —
    // what an X report (GetCashDrawerXReportQuery) summarizes mid-shift,
    // and what a close (CloseCashDrawerCommand) finalizes. Cross-service
    // references to Warehouse.Location/Identity.User, same reasoning as
    // Sale.LocationId/CashierUserId — plain ints, not real FKs.
    public class CashDrawerSession : EntityBase
    {
        public int LocationId { get; set; }
        public int CashierUserId { get; set; }

        public decimal OpeningFloat { get; set; }
        public DateTime OpenedAt { get; set; } = DateTime.UtcNow;

        // Both null while the session is open — mirrors Sale's own
        // CompletedAt/ReturnedAt "null until this state transition
        // actually happens" pattern.
        public DateTime? ClosedAt { get; set; }
        public decimal? ClosingCount { get; set; }
    }
}
