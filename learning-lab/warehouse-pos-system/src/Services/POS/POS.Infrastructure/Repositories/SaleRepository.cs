using Microsoft.EntityFrameworkCore;
using POS.Application.Contracts.Persistence;
using POS.Domain.Entities;
using POS.Infrastructure.Persistence;

namespace POS.Infrastructure.Repositories
{
    public class SaleRepository : ISaleRepository
    {
        private readonly PosContext _context;

        public SaleRepository(PosContext context)
        {
            _context = context;
        }

        public async Task<Sale?> GetById(int id)
        {
            return await _context.Sales.Include(s => s.Customer).FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<IEnumerable<Sale>> GetInProgress(int? locationId)
        {
            var query = _context.Sales.Include(s => s.Customer).Where(s => s.Status == SaleStatus.InProgress);
            if (locationId.HasValue)
            {
                query = query.Where(s => s.LocationId == locationId.Value);
            }

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<Sale>> GetCompletedSince(int locationId, DateTime sinceUtc)
        {
            return await _context.Sales
                .Where(s => s.LocationId == locationId && s.Status == SaleStatus.Completed && s.CompletedAt >= sinceUtc)
                .ToListAsync();
        }

        // Stages only — see IUnitOfWork. StartSaleCommand is the one
        // caller that also calls SaveChangesAsync right after; every
        // other handler stages this alongside a SaleLine change first.
        public async Task<Sale> AddAsync(Sale sale)
        {
            await _context.Sales.AddAsync(sale);
            return sale;
        }

        public Task UpdateAsync(Sale sale)
        {
            _context.Sales.Update(sale);
            return Task.CompletedTask;
        }
    }
}
