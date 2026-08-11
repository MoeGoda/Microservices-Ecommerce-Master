using MenuItems.API.Entities;
using MongoDB.Driver;

namespace MenuItems.API.Data
{
    // Runs once at startup so the API has something to return immediately —
    // no manual "insert some rows" step before you can try the endpoints.
    public static class MenuItemsContextSeed
    {
        public static void SeedData(IMongoCollection<MenuItem> menuItemsCollection)
        {
            var existing = menuItemsCollection.Find(m => true).Any();
            if (!existing)
            {
                menuItemsCollection.InsertMany(GetPreconfiguredMenuItems());
            }
        }

        private static IEnumerable<MenuItem> GetPreconfiguredMenuItems()
        {
            return new List<MenuItem>
            {
                new() { Id = "60a2c1f5f1d2a1a1a1a1a1a1", Name = "Margherita Pizza", Category = "Pizza", Description = "Tomato, mozzarella, basil", Price = 9.50m },
                new() { Id = "60a2c1f5f1d2a1a1a1a1a1a2", Name = "Pepperoni Pizza", Category = "Pizza", Description = "Tomato, mozzarella, pepperoni", Price = 11.00m },
                new() { Id = "60a2c1f5f1d2a1a1a1a1a1a3", Name = "Caesar Salad", Category = "Salad", Description = "Romaine, chicken, parmesan, croutons", Price = 7.50m },
                new() { Id = "60a2c1f5f1d2a1a1a1a1a1a4", Name = "Cola", Category = "Drink", Description = "330ml can", Price = 1.80m },
            };
        }
    }
}
