using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MenuItems.API.Entities
{
    // Mirrors Catalog.API's Product entity: a plain POCO that the Mongo driver
    // serializes to/from BSON. [BsonId] marks the Mongo primary key; representing
    // it as a string ObjectId keeps the API contract simple (no MongoDB types leak out).
    public class MenuItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        public string Name { get; set; } = null!;
        public string Category { get; set; } = null!;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }
}
