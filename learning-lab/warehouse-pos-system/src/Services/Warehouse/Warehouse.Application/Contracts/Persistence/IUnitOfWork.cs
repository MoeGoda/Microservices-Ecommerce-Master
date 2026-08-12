namespace Warehouse.Application.Contracts.Persistence
{
    // B1 flagged this exact gap: receiving/adjusting stock has to write a
    // StockLevel change AND a StockTransaction row together, or the ledger
    // stops matching the balance. Every Warehouse repository's Add/Update
    // method now only stages a change on the tracked DbContext — it does
    // NOT call SaveChanges itself (unlike Identity's repositories, which
    // never needed more than one entity to change per command). A handler
    // that needs several staged changes to succeed or fail together calls
    // this exactly once, at the end, so EF Core commits them all in a
    // single transaction instead of one per repository call.
    public interface IUnitOfWork
    {
        Task SaveChangesAsync();
    }
}
