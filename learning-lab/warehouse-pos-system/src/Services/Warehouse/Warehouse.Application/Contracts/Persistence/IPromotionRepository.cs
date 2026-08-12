using Warehouse.Domain.Entities;

namespace Warehouse.Application.Contracts.Persistence
{
    public interface IPromotionRepository
    {
        Task<Promotion> AddAsync(Promotion promotion);

        // The promotion in effect for this item right now, or null if
        // there isn't one. Deliberately doesn't guard against two
        // promotions overlapping the same window for the same item — that
        // would need a real conflict check at creation time, not solved
        // here; if it ever happens, the most recently STARTED one wins
        // (see the implementation), an arbitrary but at least deterministic
        // tie-break.
        Task<Promotion?> GetActiveForItem(int itemId, DateTime nowUtc);
    }
}
