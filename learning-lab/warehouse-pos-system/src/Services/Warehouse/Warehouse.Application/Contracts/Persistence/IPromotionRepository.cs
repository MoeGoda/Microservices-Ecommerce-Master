using Warehouse.Domain.Entities;

namespace Warehouse.Application.Contracts.Persistence
{
    public interface IPromotionRepository
    {
        Task<Promotion> AddAsync(Promotion promotion);
        Task<Promotion?> GetById(int id);

        // Every promotion this item has ever had, active or not,
        // cancelled or not — the browse/history view CreatePromotionCommand's
        // own README gap named ("can't later browse or cancel it through
        // this UI"). Ordered newest-started first, same convention
        // GetItemPriceHistoryQuery already uses for its own history list.
        Task<IEnumerable<Promotion>> GetAllForItem(int itemId);

        Task UpdateAsync(Promotion promotion);

        // The promotion in effect for this item right now, or null if
        // there isn't one. Deliberately doesn't guard against two
        // promotions overlapping the same window for the same item — that
        // would need a real conflict check at creation time, not solved
        // here; if it ever happens, the most recently STARTED one wins
        // (see the implementation), an arbitrary but at least deterministic
        // tie-break. Excludes cancelled promotions (see CancelPromotionCommand).
        Task<Promotion?> GetActiveForItem(int itemId, DateTime nowUtc);
    }
}
