namespace Common.Security
{
    // Mirrors Identity.Domain.Entities.RoleNames exactly — but Identity's
    // own copy stays the actual source of truth for the seeded Role
    // table rows (RoleId FK targets, IdentityContextSeed), since domain
    // ownership belongs there, not here. This copy exists because
    // [Authorize(Roles = "...")] attribute arguments must be compile-time
    // constants, and Warehouse/POS/Reporting's own controllers — which
    // have no reference to Identity.Domain at all, by the same "no shared
    // domain assemblies across services" rule this project follows
    // everywhere else — still need those exact strings to restrict their
    // own mutation endpoints by role (F2). Common.Security is already the
    // one cross-service building block for security/auth concerns (every
    // service already references it for AddJwtAuthentication), so a
    // shared, typo-proof set of role-name constants belongs here rather
    // than being hand-copied as string literals into every controller
    // that needs one.
    public static class RoleNames
    {
        public const string Admin = "Admin";
        public const string Manager = "Manager";
        public const string Cashier = "Cashier";
        public const string WarehouseStaff = "WarehouseStaff";
    }
}
