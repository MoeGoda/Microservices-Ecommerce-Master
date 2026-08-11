using MenuItems.API.Data.Interfaces;
using MenuItems.API.Entities;
using MongoDB.Driver;

namespace MenuItems.API.Data
{
    public class MenuItemsContext : IMenuItemsContext
    {
        public MenuItemsContext(IConfiguration configuration)
        {
            var client = new MongoClient(configuration.GetValue<string>("DatabaseSettings:ConnectionString"));
            var database = client.GetDatabase(configuration.GetValue<string>("DatabaseSettings:DatabaseName"));

            MenuItems = database.GetCollection<MenuItem>(configuration.GetValue<string>("DatabaseSettings:CollectionName"));

            MenuItemsContextSeed.SeedData(MenuItems);
        }

        public IMongoCollection<MenuItem> MenuItems { get; }
    }
}
