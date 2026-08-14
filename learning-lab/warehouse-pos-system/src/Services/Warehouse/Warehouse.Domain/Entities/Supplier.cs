using Warehouse.Domain.Common;

namespace Warehouse.Domain.Entities
{
    // I — who a PurchaseOrder is placed with. Deliberately deactivatable
    // (IsActive), not deletable — the same reasoning User.IsActive (A1)
    // already established: a supplier with PurchaseOrder history can't be
    // deleted without orphaning that history, but it should still be
    // possible to stop new POs from being placed with them.
    public class Supplier : EntityBase
    {
        public string Name { get; set; } = null!;
        public string? ContactName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
