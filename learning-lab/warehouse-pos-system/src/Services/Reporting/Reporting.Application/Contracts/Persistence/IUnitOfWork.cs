namespace Reporting.Application.Contracts.Persistence
{
    // Same idiom as Warehouse/POS's own IUnitOfWork — repositories only
    // stage changes; a handler that needs several staged changes to
    // succeed or fail together calls this exactly once, at the end.
    public interface IUnitOfWork
    {
        Task SaveChangesAsync();
    }
}
