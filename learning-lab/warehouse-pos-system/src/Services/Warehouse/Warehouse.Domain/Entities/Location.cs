using Warehouse.Domain.Common;

namespace Warehouse.Domain.Entities
{
    // A physical storage spot in the warehouse — an aisle/shelf/bin. Stock
    // is tracked *per item per location* (see StockLevel), not just as one
    // total per item, because "how many do we have" and "where are they"
    // are both real questions a warehouse worker scanning a barcode needs
    // answered.
    public class Location : EntityBase
    {
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
    }
}
