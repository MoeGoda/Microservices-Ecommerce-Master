using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Domain.Entities;
using Warehouse.Infrastructure.Persistence;

namespace Warehouse.Infrastructure.Repositories
{
    public class OutboxRepository : IOutboxRepository
    {
        private readonly WarehouseContext _context;

        public OutboxRepository(WarehouseContext context)
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
