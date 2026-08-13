using Microsoft.EntityFrameworkCore;
using POS.Application.Contracts.Persistence;
using POS.Domain.Entities;
using POS.Infrastructure.Persistence;

namespace POS.Infrastructure.Repositories
{
    public class OutboxRepository : IOutboxRepository
    {
        private readonly PosContext _context;

        public OutboxRepository(PosContext context)
        {
            _context = context;
        }

        public async Task<OutboxMessage> AddMessageAsync(OutboxMessage message)
        {
            await _context.OutboxMessages.AddAsync(message);
            return message;
        }

        public async Task<OutboxDelivery> AddDeliveryAsync(OutboxDelivery delivery)
        {
            await _context.OutboxDeliveries.AddAsync(delivery);
            return delivery;
        }

        public Task UpdateDeliveryAsync(OutboxDelivery delivery)
        {
            _context.OutboxDeliveries.Update(delivery);
            return Task.CompletedTask;
        }

        public async Task<IEnumerable<OutboxDelivery>> GetPendingDeliveries()
        {
            return await _context.OutboxDeliveries
                .Include(d => d.OutboxMessage)
                .Where(d => d.Status == OutboxStatus.Pending)
                .ToListAsync();
        }
    }
}
