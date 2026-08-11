using MenuItems.API.Data.Interfaces;
using MenuItems.API.Entities;
using MenuItems.API.Repositories.Interfaces;
using MongoDB.Driver;

namespace MenuItems.API.Repositories
{
    public class MenuItemsRepository : IMenuItemsRepository
    {
        private readonly IMenuItemsContext _context;

        public MenuItemsRepository(IMenuItemsContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<MenuItem?> GetMenuItem(string id)
        {
            return await _context.MenuItems.Find(m => m.Id == id).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<MenuItem>> GetMenuItems()
        {
            return await _context.MenuItems.Find(m => true).ToListAsync();
        }

        public async Task<IEnumerable<MenuItem>> GetMenuItemsByCategory(string category)
        {
            var filter = Builders<MenuItem>.Filter.Eq(m => m.Category, category);
            return await _context.MenuItems.Find(filter).ToListAsync();
        }

        public async Task CreateMenuItem(MenuItem menuItem)
        {
            await _context.MenuItems.InsertOneAsync(menuItem);
        }

        public async Task UpdateMenuItem(MenuItem menuItem)
        {
            await _context.MenuItems.ReplaceOneAsync(m => m.Id == menuItem.Id, menuItem);
        }

        public async Task<bool> DeleteMenuItem(string id)
        {
            var result = await _context.MenuItems.DeleteOneAsync(m => m.Id == id);
            return result.DeletedCount > 0;
        }
    }
}
