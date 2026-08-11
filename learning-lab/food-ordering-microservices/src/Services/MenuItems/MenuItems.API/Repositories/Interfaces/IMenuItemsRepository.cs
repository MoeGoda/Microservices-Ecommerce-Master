using MenuItems.API.Entities;

namespace MenuItems.API.Repositories.Interfaces
{
    // The Repository Pattern: controllers talk to this interface, never to
    // MongoDB.Driver directly. If we swapped Mongo for another store later,
    // only the implementation below would change — callers wouldn't notice.
    public interface IMenuItemsRepository
    {
        Task<MenuItem?> GetMenuItem(string id);
        Task<IEnumerable<MenuItem>> GetMenuItems();
        Task<IEnumerable<MenuItem>> GetMenuItemsByCategory(string category);
        Task CreateMenuItem(MenuItem menuItem);
        Task UpdateMenuItem(MenuItem menuItem);
        Task<bool> DeleteMenuItem(string id);
    }
}
