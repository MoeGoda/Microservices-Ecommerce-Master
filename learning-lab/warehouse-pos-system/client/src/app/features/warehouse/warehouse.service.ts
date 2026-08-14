import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../../shared/models/pagination.models';
import {
  AddItemBarcodeRequest,
  AddItemUnitRequest,
  AdjustStockRequest,
  CategoryDto,
  CreateItemRequest,
  CreatePromotionRequest,
  InventoryValuationLineDto,
  ItemBarcodeDto,
  ItemDetailDto,
  ItemPriceHistoryDto,
  ItemSummaryDto,
  ItemUnitDto,
  LocationDto,
  PromotionDto,
  ReceiveStockRequest,
  StockLevelDto,
  TransferStockRequest,
  TransferStockResultDto,
  UnitOfMeasureDto,
  UpdateItemPriceRequest,
} from '../../shared/models/warehouse.models';

const BASE = `${environment.apiBaseUrl}/Warehouse`;

// Every call goes through the gateway's /Warehouse/... upstream routes,
// same as AuthService going through /Identity/... — never straight to
// Warehouse.API's own port. authInterceptor already attaches the token to
// every outgoing request; this service doesn't touch auth at all.
@Injectable({ providedIn: 'root' })
export class WarehouseService {
  constructor(private readonly http: HttpClient) {}

  getItems(page = 1, pageSize = 20): Observable<PagedResult<ItemSummaryDto>> {
    return this.http.get<PagedResult<ItemSummaryDto>>(`${BASE}/Items`, { params: { page, pageSize } });
  }

  getItem(id: number): Observable<ItemDetailDto> {
    return this.http.get<ItemDetailDto>(`${BASE}/Items/${id}`);
  }

  createItem(request: CreateItemRequest): Observable<ItemDetailDto> {
    return this.http.post<ItemDetailDto>(`${BASE}/Items`, request);
  }

  addBarcode(itemId: number, request: AddItemBarcodeRequest): Observable<ItemBarcodeDto> {
    return this.http.post<ItemBarcodeDto>(`${BASE}/Items/${itemId}/barcodes`, request);
  }

  addUnit(itemId: number, request: AddItemUnitRequest): Observable<ItemUnitDto> {
    return this.http.post<ItemUnitDto>(`${BASE}/Items/${itemId}/units`, request);
  }

  getStockLevels(itemId: number): Observable<StockLevelDto[]> {
    return this.http.get<StockLevelDto[]>(`${BASE}/Stock/${itemId}`);
  }

  receiveStock(request: ReceiveStockRequest): Observable<StockLevelDto> {
    return this.http.post<StockLevelDto>(`${BASE}/Stock/receive`, request);
  }

  adjustStock(request: AdjustStockRequest): Observable<StockLevelDto> {
    return this.http.post<StockLevelDto>(`${BASE}/Stock/adjust`, request);
  }

  transferStock(request: TransferStockRequest): Observable<TransferStockResultDto> {
    return this.http.post<TransferStockResultDto>(`${BASE}/Stock/transfer`, request);
  }

  getCategories(): Observable<CategoryDto[]> {
    return this.http.get<CategoryDto[]>(`${BASE}/MasterData/categories`);
  }

  getLocations(): Observable<LocationDto[]> {
    return this.http.get<LocationDto[]>(`${BASE}/MasterData/locations`);
  }

  getUnitsOfMeasure(): Observable<UnitOfMeasureDto[]> {
    return this.http.get<UnitOfMeasureDto[]>(`${BASE}/MasterData/units-of-measure`);
  }

  updatePrice(itemId: number, request: UpdateItemPriceRequest): Observable<ItemDetailDto> {
    return this.http.put<ItemDetailDto>(`${BASE}/Items/${itemId}/price`, request);
  }

  getPriceHistory(itemId: number): Observable<ItemPriceHistoryDto[]> {
    return this.http.get<ItemPriceHistoryDto[]>(`${BASE}/Items/${itemId}/price-history`);
  }

  createPromotion(itemId: number, request: CreatePromotionRequest): Observable<PromotionDto> {
    return this.http.post<PromotionDto>(`${BASE}/Items/${itemId}/promotions`, request);
  }

  getPromotions(itemId: number): Observable<PromotionDto[]> {
    return this.http.get<PromotionDto[]>(`${BASE}/Items/${itemId}/promotions`);
  }

  cancelPromotion(itemId: number, promotionId: number): Observable<PromotionDto> {
    return this.http.post<PromotionDto>(`${BASE}/Items/${itemId}/promotions/${promotionId}/cancel`, null);
  }

  getInventoryValuation(): Observable<InventoryValuationLineDto[]> {
    return this.http.get<InventoryValuationLineDto[]>(`${BASE}/Reports/inventory-valuation`);
  }
}
