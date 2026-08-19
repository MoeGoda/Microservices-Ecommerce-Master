using POS.Domain.Common;

namespace POS.Domain.Entities
{
    // One cash-in or cash-out event within a CashDrawerSession — a
    // register action distinct from a Sale (no items, no customer),
    // e.g. topping up change or a mid-shift bank drop.
    public class CashMovement : EntityBase
    {
        public int CashDrawerSessionId { get; set; }
        public CashDrawerSession CashDrawerSession { get; set; } = null!;

        public CashMovementType Type { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; } = null!;
        public int CreatedByUserId { get; set; }
    }
}
