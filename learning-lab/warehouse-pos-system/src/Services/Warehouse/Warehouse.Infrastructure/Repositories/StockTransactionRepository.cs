using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Domain.Entities;
using Warehouse.Infrastructure.Persistence;

namespace Warehouse.Infrastructure.Repositories
{
    public class StockTransactionRepository : IStockTransactionRepository
    {
        private readonly WarehouseContext _context;

        public StockTransactionRepository(WarehouseContext context)
        {
            _context = context;
        }

        public async Task<StockTransaction> AddAsync(StockTransaction transaction)
        {
            await _context.StockTransactions.AddAsync(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }

        public async Task<IEnumerable<StockTransaction>> GetByItem(int itemId)
        {
            return await _context.StockTransactions
                .Where(t => t.ItemId == itemId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }
    }
}
