using Microsoft.EntityFrameworkCore;
using Notifications.Application.Contracts.Persistence;
using Notifications.Domain.Entities;
using Notifications.Infrastructure.Persistence;

namespace Notifications.Infrastructure.Repositories
{
    public class StockLevelSnapshotRepository : IStockLevelSnapshotRepository
    {
        private readonly NotificationsContext _context;

        public StockLevelSnapshotRepository(NotificationsContext context)
        {
            _context = context;
        }

        public async Task<StockLevelSnapshot?> GetByItemAndLocation(int itemId, int locationId)
        {
            return await _context.StockLevelSnapshots
                .FirstOrDefaultAsync(s => s.ItemId == itemId && s.LocationId == locationId);
        }

        public async Task<StockLevelSnapshot> AddAsync(StockLevelSnapshot snapshot)
        {
            await _context.StockLevelSnapshots.AddAsync(snapshot);
            return snapshot;
        }

        public Task UpdateAsync(StockLevelSnapshot snapshot)
        {
            _context.StockLevelSnapshots.Update(snapshot);
            return Task.CompletedTask;
        }
    }
}
