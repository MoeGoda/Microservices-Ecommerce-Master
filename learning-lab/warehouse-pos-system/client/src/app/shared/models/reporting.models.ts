// Mirrors Reporting.Application.Models.* — the D2 aggregated-report shapes
// returned by ReportsController, distinct from D1's raw read-model dumps
// (there is no reporting.models equivalent for those; nothing in this app
// consumes them yet).

// Mirrors SalesByDayDto. date is a plain "YYYY-MM-DD" string (System.Text.Json's
// default DateOnly format) — never parsed as a Date/time value, since a day
// bucket has no time-of-day or timezone to carry.
export interface SalesByDayDto {
  date: string;
  saleCount: number;
  total: number;
}

// Mirrors TopSellingItemDto.
export interface TopSellingItemDto {
  itemId: number;
  sku: string;
  itemName: string;
  totalQuantity: number;
  totalRevenue: number;
}

// Mirrors StockLevelRecordDto — Reporting's OWN snapshot of a stock level
// (denormalized Sku/ItemName/LocationCode/LocationName, D2), not to be
// confused with Warehouse's StockLevelDto (warehouse.models.ts): different
// service, different shape, and this one is only ever a low-stock report row.
export interface StockLevelRecordDto {
  itemId: number;
  sku: string;
  itemName: string;
  locationId: number;
  locationCode: string;
  locationName: string;
  quantityOnHand: number;
  reorderThreshold: number;
  asOfUtc: string;
}
