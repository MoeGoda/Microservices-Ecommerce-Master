import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { roleGuard } from './core/auth/role.guard';
import { LoginComponent } from './features/login/login.component';
import { ItemsListComponent } from './features/warehouse/items-list/items-list.component';
import { ItemCreateComponent } from './features/warehouse/item-create/item-create.component';
import { ItemDetailComponent } from './features/warehouse/item-detail/item-detail.component';
import { PosRegisterComponent } from './features/pos/pos-register.component';
import { ReportsDashboardComponent } from './features/reporting/reports-dashboard/reports-dashboard.component';
import { UsersAdminComponent } from './features/users/users-admin/users-admin.component';
import { PurchaseOrdersAdminComponent } from './features/purchasing/purchase-orders-admin/purchase-orders-admin.component';
import { SuppliersAdminComponent } from './features/purchasing/suppliers-admin/suppliers-admin.component';
import { ADMIN_ROLES, POS_ROLES, REPORTS_ROLES, USER_MANAGEMENT_ROLES } from './shared/models/roles';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'login' },
  { path: 'login', component: LoginComponent },
  // F2 — authGuard alone only checked "is anyone signed in"; roleGuard
  // adds "is THIS the role this area is for," matching each area's own
  // backend [Authorize(Roles = ...)] restriction one-for-one.
  // K — the former single /admin screen (create + browse + manage all on
  // one page) split into three routed screens, same guard on all three.
  { path: 'items', component: ItemsListComponent, canActivate: [authGuard, roleGuard(ADMIN_ROLES)] },
  { path: 'items/new', component: ItemCreateComponent, canActivate: [authGuard, roleGuard(ADMIN_ROLES)] },
  { path: 'items/:id', component: ItemDetailComponent, canActivate: [authGuard, roleGuard(ADMIN_ROLES)] },
  { path: 'pos', component: PosRegisterComponent, canActivate: [authGuard, roleGuard(POS_ROLES)] },
  { path: 'reports', component: ReportsDashboardComponent, canActivate: [authGuard, roleGuard(REPORTS_ROLES)] },
  { path: 'users', component: UsersAdminComponent, canActivate: [authGuard, roleGuard(USER_MANAGEMENT_ROLES)] },
  // Same role set as /items — a PO/Supplier is warehouse-management data,
  // same [Authorize(Roles = CatalogManagerRoles)] set Items/Stock use.
  { path: 'suppliers', component: SuppliersAdminComponent, canActivate: [authGuard, roleGuard(ADMIN_ROLES)] },
  { path: 'purchase-orders', component: PurchaseOrdersAdminComponent, canActivate: [authGuard, roleGuard(ADMIN_ROLES)] },
  { path: '**', redirectTo: 'login' },
];
