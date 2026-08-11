using Warehouse.Domain.Common;

namespace Warehouse.Domain.Entities
{
    // Master data the Admin Panel (B3) will manage directly — not
    // hardcoded, but also not something a Cashier or the POS screen ever
    // creates on the fly. Same "reference lookup table" shape as
    // Identity's Role.
    public class Category : EntityBase
    {
        public string Name { get; set; } = null!;
    }
}
