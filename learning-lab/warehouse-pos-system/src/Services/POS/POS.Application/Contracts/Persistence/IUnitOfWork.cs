namespace POS.Application.Contracts.Persistence
{
    // Same shape, same reasoning as Warehouse.Application's IUnitOfWork
    // (B2): repositories only stage changes on the tracked DbContext, they
    // never call SaveChanges themselves. AddSaleLineCommand needs exactly
    // this — inserting a SaleLine AND updating its parent Sale's running
    // Total have to commit together, or the two can drift out of sync
    // exactly the way an un-synchronized StockLevel/StockTransaction pair
    // would in Warehouse.
    public interface IUnitOfWork
    {
        Task SaveChangesAsync();
    }
}
