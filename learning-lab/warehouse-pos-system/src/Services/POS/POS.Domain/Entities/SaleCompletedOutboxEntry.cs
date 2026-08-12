using POS.Domain.Common;

namespace POS.Domain.Entities
{
    // The Outbox pattern's whole reason to exist: CheckoutCommandHandler
    // (C1/C3) writes this row in the SAME SaveChanges call that marks the
    // Sale Completed, so "the sale completed" and "an event was queued to
    // tell Warehouse" either both happen or neither does — there is no
    // moment where one is true and the other isn't, even if the process
    // crashes right after the commit. A background dispatcher (not
    // registered as a hosted service yet — see SaleCompletedOutboxBackgroundService)
    // polls for Pending rows and delivers them; it is NOT part of the
    // checkout request/response cycle, which is what makes this genuinely
    // asynchronous rather than just a slow synchronous call.
    //
    // Deliberately specific to one event type (SaleId/LocationId/LinesJson)
    // rather than a generic Type+Payload envelope — there is exactly one
    // kind of outgoing event in this system so far. A generic outbox is
    // the natural next step the moment a SECOND event type shows up (D1's
    // reporting events, E1's notifications), not before.
    public class SaleCompletedOutboxEntry : EntityBase
    {
        public int SaleId { get; set; }
        public int LocationId { get; set; }

        // A small JSON array of { itemId, quantity } — write-once (at
        // checkout), read-once (by the dispatcher). Not worth a related
        // SaleCompletedOutboxLine table for something nothing ever
        // queries by individual line.
        public string LinesJson { get; set; } = null!;

        public OutboxStatus Status { get; set; } = OutboxStatus.Pending;
        public int Attempts { get; set; }
        public string? LastError { get; set; }
        public DateTime? ProcessedAtUtc { get; set; }
    }
}
