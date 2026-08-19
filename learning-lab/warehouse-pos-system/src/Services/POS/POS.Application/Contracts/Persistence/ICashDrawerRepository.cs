using POS.Domain.Entities;

namespace POS.Application.Contracts.Persistence
{
    public interface ICashDrawerRepository
    {
        Task<CashDrawerSession?> GetSessionById(int id);

        // The one currently-open session for a location, if any — a
        // cashier can't record a cash movement or pull an X report
        // without knowing which open session it belongs to.
        Task<CashDrawerSession?> GetOpenSession(int locationId);

        Task<CashDrawerSession> AddSessionAsync(CashDrawerSession session);
        Task UpdateSessionAsync(CashDrawerSession session);

        Task<IEnumerable<CashMovement>> GetMovements(int sessionId);
        Task<CashMovement> AddMovementAsync(CashMovement movement);
    }
}
