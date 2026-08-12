// Mirrors POS.Application.Models.* and the Features/Sales/Commands/*
// request shapes (backend, C1/C2/C3) — property names match ASP.NET
// Core's default camelCase JSON serialization, same convention as
// warehouse.models.ts.

export interface SaleLineDto {
  id: number;
  itemId: number;
  sku: string;
  itemName: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

// There is no separate "receipt" shape on the backend — a Completed
// SaleDto (with its Lines) already IS the receipt; the Angular receipt
// view below is just this same DTO rendered once Status is Completed.
export interface SaleDto {
  id: number;
  locationId: number;
  cashierUserId: number;
  status: 'InProgress' | 'Completed' | 'Cancelled';
  total: number;
  completedAt: string | null;
  stockSyncStatus: 'Pending' | 'Synced' | 'Failed';
  lines: SaleLineDto[];
}

// Mirrors StartSaleCommand — minus CashierUserId. POS.API's SalesController
// fills that in itself from the caller's own JWT (ClaimTypes.NameIdentifier)
// rather than trusting whatever a client claims, so there's nothing for
// this request shape to carry for it.
export interface StartSaleRequest {
  locationId: number;
}

// Mirrors AddSaleLineCommand, minus SaleId — the sale is already the
// route's own {id}, so POS.API's controller fills it in from there.
export interface AddSaleLineRequest {
  barcode: string;
  quantity: number;
}
