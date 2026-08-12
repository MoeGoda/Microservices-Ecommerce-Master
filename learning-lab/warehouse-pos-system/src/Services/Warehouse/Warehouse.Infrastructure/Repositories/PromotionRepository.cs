using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Domain.Entities;
using Warehouse.Infrastructure.Persistence;

namespace Warehouse.Infrastructure.Repositories
{
    public class PromotionRepository : IPromotionRepository
    {
        private readonly WarehouseContext _context;

        public PromotionRepository(WarehouseContext context)
        {
            _context = context;
        }

        public async Task<Promotion> AddAsync(Promotion promotion)
        {
            await _context.Promotions.AddAsync(promotion);
            return promotion;
        }

        public async Task<Promotion?> GetActiveForItem(int itemId, DateTime nowUtc)
        {
            return await _context.Promotions
                .Where(p => p.ItemId == itemId && p.StartsAtUtc <= nowUtc && p.EndsAtUtc >= nowUtc)
                .OrderByDescending(p => p.StartsAtUtc)
                .FirstOrDefaultAsync();
        }
    }
}
