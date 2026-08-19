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
  // Both null unless a Promotion (C5) was active in Warehouse the moment
  // this line was added — unitPrice above is already the discounted
  // price either way; these are purely "was X, now Y" receipt detail.
  originalUnitPrice: number | null;
  promotionId: number | null;
  // Cashier-entered — never set alongside promotionId (the two are
  // mutually exclusive on the backend).
  manualDiscountPercent: number | null;
  quantity: number;
  lineTotal: number;
}

// There is no separate "receipt" shape on the backend — a Completed
// SaleDto (with its Lines) already IS the receipt; the Angular receipt
// view below is just this same DTO rendered once Status is Completed.
export interface SaleDto {
  id: number;
  // Derived from Id ("POS-000123"), not a separately stored/editable field.
  documentNumber: string;
  locationId: number;
  cashierUserId: number;
  customerId: number | null;
  customerName: string | null;
  status: 'InProgress' | 'Completed' | 'Cancelled' | 'Returned';
  manualReceiptDiscountPercent: number | null;
  isTaxExempt: boolean;
  netTotal: number;
  taxAmount: number;
  total: number;
  completedAt: string | null;
  // Set only once Status transitions to Returned — mirrors completedAt's
  // own role one step later in the sale's lifecycle (backend, Sale.ReturnedAt).
  returnedAt: string | null;
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
  manualDiscountPercent?: number | null;
}

export interface CustomerDto {
  id: number;
  name: string;
  phone: string | null;
  email: string | null;
  loyaltyPoints: number;
  balance: number;
}

export interface CreateCustomerRequest {
  name: string;
  phone?: string | null;
  email?: string | null;
}

export interface UpdateCustomerRequest {
  name: string;
  phone?: string | null;
  email?: string | null;
}

export interface AdjustCustomerBalanceRequest {
  delta: number;
  reason: string;
}

export interface CashDrawerSessionDto {
  id: number;
  locationId: number;
  cashierUserId: number;
  openingFloat: number;
  openedAt: string;
  closedAt: string | null;
  closingCount: number | null;
}

export interface CashMovementDto {
  id: number;
  type: 'CashIn' | 'CashOut';
  amount: number;
  reason: string;
  createdAt: string;
}

export interface CashDrawerXReportDto {
  sessionId: number;
  openedAt: string;
  openingFloat: number;
  cashInTotal: number;
  cashOutTotal: number;
  completedSaleCount: number;
  salesTotal: number;
  expectedCashInDrawer: number;
}
