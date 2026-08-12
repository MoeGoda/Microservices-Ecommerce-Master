using Microsoft.EntityFrameworkCore;
using POS.Application.Contracts.Persistence;
using POS.Domain.Entities;
using POS.Infrastructure.Persistence;

namespace POS.Infrastructure.Repositories
{
    public class SaleCompletedOutboxRepository : ISaleCompletedOutboxRepository
    {
        private readonly PosContext _context;

        public SaleCompletedOutboxRepository(PosContext context)
        {
            _context = context;
        }

        // Stages only — see IUnitOfWork. CheckoutCommandHandler commits
        // this together with the Sale's own Status change.
        public async Task<SaleCompletedOutboxEntry> AddAsync(SaleCompletedOutboxEntry entry)
        {
            await _context.SaleCompletedOutboxEntries.AddAsync(entry);
            return entry;
        }

        public Task UpdateAsync(SaleCompletedOutboxEntry entry)
        {
            _context.SaleCompletedOutboxEntries.Update(entry);
            return Task.CompletedTask;
        }

        public async Task<IEnumerable<SaleCompletedOutboxEntry>> GetPending()
        {
            return await _context.SaleCompletedOutboxEntries
                .Where(e => e.Status == OutboxStatus.Pending)
                .ToListAsync();
        }
    }
}
