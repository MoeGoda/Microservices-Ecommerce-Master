using POS.Domain.Entities;

namespace POS.Application.Contracts.Persistence
{
    public interface ISaleRepository
    {
        Task<Sale?> GetById(int id);

        // "Held sales" — every InProgress sale at a location, so a
        // cashier can save one aside and resume a different one later.
        // Nothing in StartSaleCommandHandler ever blocked having more
        // than one at once; this is the query that was missing to list
        // them back.
        Task<IEnumerable<Sale>> GetInProgress(int? locationId);

        // Feeds the cash-drawer X report's "completed-sale total since the
        // drawer opened" figure — Completed sales only, at one location,
        // from a point in time forward.
        Task<IEnumerable<Sale>> GetCompletedSince(int locationId, DateTime sinceUtc);

        Task<Sale> AddAsync(Sale sale);
        Task UpdateAsync(Sale sale);
    }
}
