// Mirrors Warehouse.Application.Models.* and the Features/*/Commands/*
// request shapes (backend, B1/B2) — property names match ASP.NET Core's
// default camelCase JSON serialization, same convention as auth.models.ts.

export interface CategoryDto {
  id: number;
  name: string;
}

export interface LocationDto {
  id: number;
  code: string;
  name: string;
}

export interface UnitOfMeasureDto {
  id: number;
  code: string;
  name: string;
}

export interface ItemBarcodeDto {
  id: number;
  barcode: string;
  barcodeType: string;
  isPrimary: boolean;
}

export interface ItemUnitDto {
  id: number;
  unitOfMeasureId: number;
  unitOfMeasureCode: string;
  conversionFactor: number;
}

// The list-view shape (GetAllItemsQuery, GetItemVariantsQuery) — no
// barcodes/units/variants, see ItemDetailDto for why they're kept separate.
export interface ItemSummaryDto {
  id: number;
  sku: string;
  name: string;
  unitPrice: number;
  isActive: boolean;
  categoryId: number;
  categoryName: string;
  baseUnitOfMeasureId: number;
  baseUnitOfMeasureCode: string;
  parentItemId: number | null;
}

// The detail shape (GetItemByIdQuery, ResolveBarcodeQuery) — everything
// ItemSummaryDto has, plus the collections that cost an extra fetch per item.
export interface ItemDetailDto extends ItemSummaryDto {
  description: string;
  barcodes: ItemBarcodeDto[];
  units: ItemUnitDto[];
  variants: ItemSummaryDto[];
  // Both null unless a Promotion (C5) is active right now — unitPrice
  // (inherited from ItemSummaryDto) is already the discounted price in
  // that case; these two are purely for "was X, now Y" display.
  originalUnitPrice: number | null;
  activePromotionId: number | null;
}

// Mirrors ItemPriceHistoryDto — one row per actual price change.
export interface ItemPriceHistoryDto {
  id: number;
  oldPrice: number;
  newPrice: number;
  changedAtUtc: string;
}

// Mirrors PromotionDto. discountType is 'PercentageOff' | 'FixedAmountOff'.
export interface PromotionDto {
  id: number;
  itemId: number;
  discountType: string;
  discountValue: number;
  startsAtUtc: string;
  endsAtUtc: string;
  isCancelled: boolean;
}

export interface StockLevelDto {
  itemId: number;
  locationId: number;
  locationCode: string;
  locationName: string;
  quantityOnHand: number;
  reorderThreshold: number;
  unitOfMeasureCode: string;
}

// Mirrors CreateItemCommand. The first barcode is required and always
// becomes primary — see AddItemBarcodeRequest for every barcode after it.
export interface CreateItemRequest {
  sku: string;
  name: string;
  description?: string;
  unitPrice: number;
  categoryId: number;
  baseUnitOfMeasureId: number;
  parentItemId?: number | null;
  barcode: string;
  barcodeType: string;
}

// Mirrors AddItemBarcodeCommand.
export interface AddItemBarcodeRequest {
  barcode: string;
  barcodeType: string;
  isPrimary: boolean;
}

// Mirrors AddItemUnitCommand.
export interface AddItemUnitRequest {
  unitOfMeasureId: number;
  conversionFactor: number;
}

// Mirrors ReceiveStockCommand. quantity/unitOfMeasureId is whatever unit
// the goods arrived in — the backend converts to the item's base unit.
export interface ReceiveStockRequest {
  itemId: number;
  locationId: number;
  quantity: number;
  unitOfMeasureId: number;
  reference?: string;
}

// Mirrors AdjustStockCommand. quantityChange is signed and always in the
// item's base unit — there is no unit conversion on this path.
export interface AdjustStockRequest {
  itemId: number;
  locationId: number;
  quantityChange: number;
  reference?: string;
}

// Mirrors TransferStockCommand.
export interface TransferStockRequest {
  itemId: number;
  fromLocationId: number;
  toLocationId: number;
  quantity: number;
  reference?: string;
}

// Mirrors TransferStockResultDto — both sides of the move, so the caller
// can show the new balance at the source and the destination at once.
export interface TransferStockResultDto {
  from: StockLevelDto;
  to: StockLevelDto;
}

// Mirrors Warehouse.Domain.Entities.BarcodeType.
export const BARCODE_TYPES = ['EAN13', 'EAN8', 'UPC', 'Code128', 'QRCode', 'Other'] as const;

// Mirrors UpdateItemPriceCommand.
export interface UpdateItemPriceRequest {
  newPrice: number;
}

// Mirrors CreatePromotionCommand.
export interface CreatePromotionRequest {
  discountType: string;
  discountValue: number;
  startsAtUtc: string;
  endsAtUtc: string;
}

// Mirrors Warehouse.Domain.Entities.DiscountType.
export const DISCOUNT_TYPES = ['PercentageOff', 'FixedAmountOff'] as const;

// J — mirrors InventoryValuationLineDto. Valued at the item's current
// selling price, not a purchase cost — see the backend DTO's own comment.
export interface InventoryValuationLineDto {
  itemId: number;
  sku: string;
  itemName: string;
  categoryName: string;
  totalQuantityOnHand: number;
  unitPrice: number;
  totalValue: number;
}
