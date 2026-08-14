// Mirrors Common.Security.RoleNames / Identity.Domain.Entities.RoleNames
// (backend) — the same four seeded role names, duplicated here for the
// same reason Common.Security's own copy exists: there's no shared
// runtime code between the Angular client and any backend service, so
// route guards and the toolbar's nav-link visibility need their own
// literal copy of the role names they check against.
export const ROLES = {
  Admin: 'Admin',
  Manager: 'Manager',
  Cashier: 'Cashier',
  WarehouseStaff: 'WarehouseStaff',
} as const;

export const ADMIN_ROLES = [ROLES.Admin, ROLES.Manager, ROLES.WarehouseStaff];
export const POS_ROLES = [ROLES.Admin, ROLES.Manager, ROLES.Cashier];
export const REPORTS_ROLES = [ROLES.Admin, ROLES.Manager];
// H — mirrors UsersController's own [Authorize(Roles = RoleNames.Admin)]
// exactly; unlike the other three areas, there is no Manager/staff carve-out.
export const USER_MANAGEMENT_ROLES = [ROLES.Admin];
