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

        public async Task<Promotion?> GetById(int id)
        {
            return await _context.Promotions.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<Promotion>> GetAllForItem(int itemId)
        {
            return await _context.Promotions
                .Where(p => p.ItemId == itemId)
                .OrderByDescending(p => p.StartsAtUtc)
                .ToListAsync();
        }

        public Task UpdateAsync(Promotion promotion)
        {
            _context.Promotions.Update(promotion);
            return Task.CompletedTask;
        }

        public async Task<Promotion?> GetActiveForItem(int itemId, DateTime nowUtc)
        {
            return await _context.Promotions
                .Where(p => p.ItemId == itemId && !p.IsCancelled && p.StartsAtUtc <= nowUtc && p.EndsAtUtc >= nowUtc)
                .OrderByDescending(p => p.StartsAtUtc)
                .FirstOrDefaultAsync();
        }
    }
}
