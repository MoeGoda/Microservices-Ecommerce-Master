using MenuItems.API.Entities;
using MongoDB.Driver;

namespace MenuItems.API.Data.Interfaces
{
    // Abstracting the Mongo database behind an interface (like Catalog.API's
    // ICatalogContext) means the repository below depends on "give me the
    // MenuItems collection", not on MongoClient/MongoDatabase directly. That's
    // what lets us fake IMenuItemsContext in a unit test without a real Mongo.
    public interface IMenuItemsContext
    {
        IMongoCollection<MenuItem> MenuItems { get; }
    }
}
