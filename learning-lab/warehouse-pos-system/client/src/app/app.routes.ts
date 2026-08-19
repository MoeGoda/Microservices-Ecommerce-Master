import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { roleGuard } from './core/auth/role.guard';
import { LoginComponent } from './features/login/login.component';
import { ItemsListComponent } from './features/warehouse/items-list/items-list.component';
import { ItemCreateComponent } from './features/warehouse/item-create/item-create.component';
import { ItemDetailComponent } from './features/warehouse/item-detail/item-detail.component';
import { PosRegisterComponent } from './features/pos/pos-register.component';
import { CustomersAdminComponent } from './features/pos/customers-admin/customers-admin.component';
import { ReportsDashboardComponent } from './features/reporting/reports-dashboard/reports-dashboard.component';
import { SalesByDayComponent } from './features/reporting/sales-by-day/sales-by-day.component';
import { TopSellingComponent } from './features/reporting/top-selling/top-selling.component';
import { LowStockComponent } from './features/reporting/low-stock/low-stock.component';
import { SalesLedgerComponent } from './features/reporting/sales-ledger/sales-ledger.component';
import { CashierPerformanceComponent } from './features/reporting/cashier-performance/cashier-performance.component';
import { StockMovementsReportComponent } from './features/reporting/stock-movements-report/stock-movements-report.component';
import { InventoryValuationComponent } from './features/reporting/inventory-valuation/inventory-valuation.component';
import { PurchaseOrderAgingComponent } from './features/reporting/purchase-order-aging/purchase-order-aging.component';
import { UsersAdminComponent } from './features/users/users-admin/users-admin.component';
import { PurchaseOrdersAdminComponent } from './features/purchasing/purchase-orders-admin/purchase-orders-admin.component';
import { SuppliersAdminComponent } from './features/purchasing/suppliers-admin/suppliers-admin.component';
import { WarehouseDashboardComponent } from './features/warehouse/warehouse-dashboard/warehouse-dashboard.component';
import { ReceiptsListComponent } from './features/warehouse/receipts/receipts-list.component';
import { TransfersListComponent } from './features/warehouse/transfers/transfers-list.component';
import { IssuesListComponent } from './features/warehouse/issues/issues-list.component';
import { InventoryListComponent } from './features/warehouse/inventory/inventory-list.component';
import { AdjustmentsListComponent } from './features/warehouse/adjustments/adjustments-list.component';
import { StockCountsPlaceholderComponent } from './features/warehouse/stock-counts/stock-counts-placeholder.component';
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
  // T9 — customers are POS data (loyalty/balance only mean anything in
  // the context of a sale), so this uses POS_ROLES, the same guard as
  // /pos, not ADMIN_ROLES.
  { path: 'customers', component: CustomersAdminComponent, canActivate: [authGuard, roleGuard(POS_ROLES)] },
  { path: 'reports', component: ReportsDashboardComponent, canActivate: [authGuard, roleGuard(REPORTS_ROLES)] },
  // N — the former single flat /reports page, split into its own
  // routed screen per widget (same reasoning as K's Items split),
  // matching nav-config.ts's Reports group children one-for-one.
  { path: 'reports/sales-by-day', component: SalesByDayComponent, canActivate: [authGuard, roleGuard(REPORTS_ROLES)] },
  { path: 'reports/top-selling', component: TopSellingComponent, canActivate: [authGuard, roleGuard(REPORTS_ROLES)] },
  { path: 'reports/low-stock', component: LowStockComponent, canActivate: [authGuard, roleGuard(REPORTS_ROLES)] },
  { path: 'reports/sales-ledger', component: SalesLedgerComponent, canActivate: [authGuard, roleGuard(REPORTS_ROLES)] },
  { path: 'reports/cashier-performance', component: CashierPerformanceComponent, canActivate: [authGuard, roleGuard(REPORTS_ROLES)] },
  { path: 'reports/stock-movements', component: StockMovementsReportComponent, canActivate: [authGuard, roleGuard(REPORTS_ROLES)] },
  { path: 'reports/inventory-valuation', component: InventoryValuationComponent, canActivate: [authGuard, roleGuard(REPORTS_ROLES)] },
  { path: 'reports/purchase-order-aging', component: PurchaseOrderAgingComponent, canActivate: [authGuard, roleGuard(REPORTS_ROLES)] },
  { path: 'users', component: UsersAdminComponent, canActivate: [authGuard, roleGuard(USER_MANAGEMENT_ROLES)] },
  // Same role set as /items — a PO/Supplier is warehouse-management data,
  // same [Authorize(Roles = CatalogManagerRoles)] set Items/Stock use.
  { path: 'suppliers', component: SuppliersAdminComponent, canActivate: [authGuard, roleGuard(ADMIN_ROLES)] },
  { path: 'purchase-orders', component: PurchaseOrdersAdminComponent, canActivate: [authGuard, roleGuard(ADMIN_ROLES)] },
  // M — the new Warehouse group's own screens (Dashboard/Receipts/
  // Transfers/Issues/Inventory/Adjustments/Stock Counts), same guard set
  // as /items since these are the same warehouse-management data.
  { path: 'warehouse', component: WarehouseDashboardComponent, canActivate: [authGuard, roleGuard(ADMIN_ROLES)] },
  { path: 'warehouse/receipts', component: ReceiptsListComponent, canActivate: [authGuard, roleGuard(ADMIN_ROLES)] },
  { path: 'warehouse/transfers', component: TransfersListComponent, canActivate: [authGuard, roleGuard(ADMIN_ROLES)] },
  { path: 'warehouse/issues', component: IssuesListComponent, canActivate: [authGuard, roleGuard(ADMIN_ROLES)] },
  { path: 'warehouse/inventory', component: InventoryListComponent, canActivate: [authGuard, roleGuard(ADMIN_ROLES)] },
  { path: 'warehouse/adjustments', component: AdjustmentsListComponent, canActivate: [authGuard, roleGuard(ADMIN_ROLES)] },
  { path: 'warehouse/stock-counts', component: StockCountsPlaceholderComponent, canActivate: [authGuard, roleGuard(ADMIN_ROLES)] },
  { path: '**', redirectTo: 'login' },
];
