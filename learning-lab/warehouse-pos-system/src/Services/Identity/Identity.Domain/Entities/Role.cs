using Identity.Domain.Common;

namespace Identity.Domain.Entities
{
    // Deliberately simple for now: a lookup table of role names.
    // A user has exactly one Role (see User.RoleId) rather than a many-to-many
    // Users<->Roles table. That's the honest tradeoff of a first cut — easy to
    // reason about, easy to query "who can do X". If a real requirement shows
    // up for a user needing multiple roles at once, this is the seam where
    // you'd introduce a UserRole join entity instead of rewriting everything.
    public class Role : EntityBase
    {
        public string Name { get; set; } = null!;
    }

    // Seeded role names — kept as constants so callers don't scatter magic
    // strings like "Admin" across the codebase.
    public static class RoleNames
    {
        public const string Admin = "Admin";
        public const string Manager = "Manager";
        public const string Cashier = "Cashier";
        public const string WarehouseStaff = "WarehouseStaff";
    }
}
