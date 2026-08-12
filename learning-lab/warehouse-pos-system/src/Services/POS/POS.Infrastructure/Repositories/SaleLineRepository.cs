using Microsoft.EntityFrameworkCore;
using POS.Application.Contracts.Persistence;
using POS.Domain.Entities;
using POS.Infrastructure.Persistence;

namespace POS.Infrastructure.Repositories
{
    public class SaleLineRepository : ISaleLineRepository
    {
        private readonly PosContext _context;

        public SaleLineRepository(PosContext context)
        {
            _context = context;
        }

        public async Task<SaleLine?> GetById(int id)
        {
            return await _context.SaleLines.FirstOrDefaultAsync(l => l.Id == id);
        }

        public async Task<IEnumerable<SaleLine>> GetBySale(int saleId)
        {
            return await _context.SaleLines.Where(l => l.SaleId == saleId).ToListAsync();
        }

        // Stages only — see IUnitOfWork.
        public async Task<SaleLine> AddAsync(SaleLine saleLine)
        {
            await _context.SaleLines.AddAsync(saleLine);
            return saleLine;
        }

        public Task DeleteAsync(SaleLine saleLine)
        {
            _context.SaleLines.Remove(saleLine);
            return Task.CompletedTask;
        }
    }
}
