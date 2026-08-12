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
            return await _context.Sales.FirstOrDefaultAsync(s => s.Id == id);
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
