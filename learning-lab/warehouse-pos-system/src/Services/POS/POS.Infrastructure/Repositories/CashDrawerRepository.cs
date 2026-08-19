using Microsoft.EntityFrameworkCore;
using POS.Application.Contracts.Persistence;
using POS.Domain.Entities;
using POS.Infrastructure.Persistence;

namespace POS.Infrastructure.Repositories
{
    public class CashDrawerRepository : ICashDrawerRepository
    {
        private readonly PosContext _context;

        public CashDrawerRepository(PosContext context)
        {
            _context = context;
        }

        public async Task<CashDrawerSession?> GetSessionById(int id)
        {
            return await _context.CashDrawerSessions.FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<CashDrawerSession?> GetOpenSession(int locationId)
        {
            return await _context.CashDrawerSessions
                .Where(s => s.LocationId == locationId && s.ClosedAt == null)
                .FirstOrDefaultAsync();
        }

        public async Task<CashDrawerSession> AddSessionAsync(CashDrawerSession session)
        {
            await _context.CashDrawerSessions.AddAsync(session);
            return session;
        }

        public Task UpdateSessionAsync(CashDrawerSession session)
        {
            _context.CashDrawerSessions.Update(session);
            return Task.CompletedTask;
        }

        public async Task<IEnumerable<CashMovement>> GetMovements(int sessionId)
        {
            return await _context.CashMovements
                .Where(m => m.CashDrawerSessionId == sessionId)
                .ToListAsync();
        }

        public async Task<CashMovement> AddMovementAsync(CashMovement movement)
        {
            await _context.CashMovements.AddAsync(movement);
            return movement;
        }
    }
}
