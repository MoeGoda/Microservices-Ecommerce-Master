// Mirrors Warehouse.Application.Models.* (Suppliers/PurchaseOrders, I) and
// their Commands/* request shapes — same camelCase convention as
// warehouse.models.ts.

export interface SupplierDto {
  id: number;
  name: string;
  contactName: string | null;
  email: string | null;
  phone: string | null;
  address: string | null;
  isActive: boolean;
}

// Mirrors CreateSupplierCommand.
export interface CreateSupplierRequest {
  name: string;
  contactName?: string;
  email?: string;
  phone?: string;
  address?: string;
}

// Mirrors Warehouse.Domain.Entities.PurchaseOrderStatus.
export type PurchaseOrderStatus = 'Draft' | 'Ordered' | 'PartiallyReceived' | 'Received' | 'Cancelled';

export interface PurchaseOrderLineDto {
  id: number;
  itemId: number;
  itemSku: string;
  itemName: string;
  unitOfMeasureId: number;
  unitOfMeasureCode: string;
  orderedQuantity: number;
  receivedQuantity: number;
  unitCost: number;
}

// The list-view shape (GetPurchaseOrdersQuery) — no lines, see
// PurchaseOrderDetailDto for why they're kept separate.
export interface PurchaseOrderSummaryDto {
  id: number;
  orderNumber: string;
  supplierId: number;
  supplierName: string;
  status: PurchaseOrderStatus;
  createdAt: string;
  orderedAtUtc: string | null;
  lineCount: number;
  totalCost: number;
}

export interface PurchaseOrderDetailDto {
  id: number;
  orderNumber: string;
  supplierId: number;
  supplierName: string;
  status: PurchaseOrderStatus;
  notes: string | null;
  createdByUserId: number;
  createdAt: string;
  orderedAtUtc: string | null;
  lines: PurchaseOrderLineDto[];
}

// Mirrors CreatePurchaseOrderCommand/CreatePurchaseOrderLineRequest.
export interface CreatePurchaseOrderLineRequest {
  itemId: number;
  unitOfMeasureId: number;
  orderedQuantity: number;
  unitCost: number;
}

export interface CreatePurchaseOrderRequest {
  supplierId: number;
  notes?: string;
  lines: CreatePurchaseOrderLineRequest[];
}

// Mirrors ReceivePurchaseOrderLineCommand (PurchaseOrderId/PurchaseOrderLineId
// come from the URL, not the body — see PurchasingService.receiveLine).
export interface ReceivePurchaseOrderLineRequest {
  locationId: number;
  quantity: number;
  reference?: string;
}

// J — mirrors PurchaseOrderAgingLineDto.
export interface PurchaseOrderAgingLineDto {
  id: number;
  orderNumber: string;
  supplierName: string;
  status: PurchaseOrderStatus;
  orderedAtUtc: string | null;
  ageDaysSinceOrdered: number | null;
  totalCost: number;
  receivedValue: number;
}
