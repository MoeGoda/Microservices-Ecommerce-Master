import { ADMIN_ROLES, POS_ROLES, REPORTS_ROLES, USER_MANAGEMENT_ROLES } from '../../shared/models/roles';

// M — the sidenav used to hardcode one hand-rolled accordion group
// (Warehouse: Items/Suppliers/Purchase Orders) directly in app.html.
// This config generalizes that into N groups so a real "Warehouse"
// section (Items, Dashboard, Receipts, Transfers, Issues, Inventory,
// Adjustments, Stock Counts) and a new "Purchasing" section (Suppliers,
// Purchase Orders) can both exist without hand-writing a second toggle
// signal per group. Every route/role pairing here matches what
// app.routes.ts already guards — this config only changes how they're
// presented in the sidebar, not what's reachable or by whom.
export interface NavLinkEntry {
  readonly kind: 'link';
  readonly labelKey: string;
  readonly route: string;
  readonly icon: string;
  readonly roles: readonly string[];
}

export interface NavChildLink {
  readonly labelKey: string;
  readonly route: string;
  readonly icon: string;
  // Only the Warehouse group's own "Dashboard" child needs this: its
  // route (/warehouse) is a literal prefix of every sibling's route
  // (/warehouse/receipts, etc.), so routerLinkActive's default
  // non-exact matching would keep it highlighted on every other child's
  // page too unless it opts into exact matching.
  readonly exact?: boolean;
}

export interface NavGroupEntry {
  readonly kind: 'group';
  readonly id: string;
  readonly labelKey: string;
  readonly icon: string;
  readonly roles: readonly string[];
  readonly children: readonly NavChildLink[];
}

// S3 — a plain, non-interactive section label (Material Admin Pro's own
// "Interface"/"UI Toolkit" rows), grouping the groups/links below it
// until the next category. Deliberately has no `roles` of its own —
// App.visibleNavEntries only keeps a category in the rendered list once
// it's confirmed at least one entry under it is visible to the current
// user, so it never introduces a role check of its own to get wrong.
export interface NavCategoryEntry {
  readonly kind: 'category';
  readonly labelKey: string;
}

export type NavEntry = NavLinkEntry | NavGroupEntry | NavCategoryEntry;

export const NAV_ENTRIES: readonly NavEntry[] = [
  { kind: 'category', labelKey: 'toolbar.categoryOperations' },
  {
    kind: 'group',
    id: 'warehouse',
    labelKey: 'toolbar.warehouseGroup',
    icon: 'inventory_2',
    roles: ADMIN_ROLES,
    children: [
      { labelKey: 'toolbar.items', route: '/items', icon: 'category' },
      { labelKey: 'toolbar.warehouseDashboard', route: '/warehouse', icon: 'dashboard', exact: true },
      { labelKey: 'toolbar.receipts', route: '/warehouse/receipts', icon: 'move_to_inbox' },
      { labelKey: 'toolbar.transfers', route: '/warehouse/transfers', icon: 'compare_arrows' },
      { labelKey: 'toolbar.issues', route: '/warehouse/issues', icon: 'outbox' },
      { labelKey: 'toolbar.inventory', route: '/warehouse/inventory', icon: 'inventory' },
      { labelKey: 'toolbar.adjustments', route: '/warehouse/adjustments', icon: 'tune' },
      { labelKey: 'toolbar.stockCounts', route: '/warehouse/stock-counts', icon: 'fact_check' },
    ],
  },
  {
    kind: 'group',
    id: 'purchasing',
    labelKey: 'toolbar.purchasingGroup',
    icon: 'storefront',
    roles: ADMIN_ROLES,
    children: [
      { labelKey: 'toolbar.suppliers', route: '/suppliers', icon: 'local_shipping' },
      { labelKey: 'toolbar.purchaseOrders', route: '/purchase-orders', icon: 'receipt_long' },
    ],
  },
  { kind: 'link', labelKey: 'toolbar.pos', route: '/pos', icon: 'point_of_sale', roles: POS_ROLES },
  { kind: 'category', labelKey: 'toolbar.categoryInsights' },
  // N — the former single flat "Reports" link, generalized the same way
  // K's one Warehouse toggle became a group in Phase M: eight separate
  // report screens instead of one 970-line page combining all of them.
  {
    kind: 'group',
    id: 'reports',
    labelKey: 'toolbar.reportsGroup',
    icon: 'bar_chart',
    roles: REPORTS_ROLES,
    children: [
      { labelKey: 'toolbar.reportsDashboard', route: '/reports', icon: 'dashboard', exact: true },
      { labelKey: 'toolbar.salesByDay', route: '/reports/sales-by-day', icon: 'show_chart' },
      { labelKey: 'toolbar.topSelling', route: '/reports/top-selling', icon: 'trending_up' },
      { labelKey: 'toolbar.lowStockReport', route: '/reports/low-stock', icon: 'inventory_2' },
      { labelKey: 'toolbar.salesLedger', route: '/reports/sales-ledger', icon: 'receipt_long' },
      { labelKey: 'toolbar.cashierPerformance', route: '/reports/cashier-performance', icon: 'badge' },
      { labelKey: 'toolbar.stockMovementsReport', route: '/reports/stock-movements', icon: 'swap_horiz' },
      { labelKey: 'toolbar.inventoryValuation', route: '/reports/inventory-valuation', icon: 'account_balance_wallet' },
      { labelKey: 'toolbar.poAging', route: '/reports/purchase-order-aging', icon: 'schedule' },
    ],
  },
  { kind: 'category', labelKey: 'toolbar.categoryAdministration' },
  { kind: 'link', labelKey: 'toolbar.users', route: '/users', icon: 'group', roles: USER_MANAGEMENT_ROLES },
];
