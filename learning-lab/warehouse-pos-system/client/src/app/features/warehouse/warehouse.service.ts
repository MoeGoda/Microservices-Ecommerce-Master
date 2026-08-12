import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AddItemBarcodeRequest,
  AddItemUnitRequest,
  AdjustStockRequest,
  CategoryDto,
  CreateItemRequest,
  ItemBarcodeDto,
  ItemDetailDto,
  ItemSummaryDto,
  ItemUnitDto,
  LocationDto,
  ReceiveStockRequest,
  StockLevelDto,
  UnitOfMeasureDto,
} from '../../shared/models/warehouse.models';

const BASE = `${environment.apiBaseUrl}/Warehouse`;

// Every call goes through the gateway's /Warehouse/... upstream routes,
// same as AuthService going through /Identity/... — never straight to
// Warehouse.API's own port. authInterceptor already attaches the token to
// every outgoing request; this service doesn't touch auth at all.
@Injectable({ providedIn: 'root' })
export class WarehouseService {
  constructor(private readonly http: HttpClient) {}

  getItems(): Observable<ItemSummaryDto[]> {
    return this.http.get<ItemSummaryDto[]>(`${BASE}/Items`);
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

  getCategories(): Observable<CategoryDto[]> {
    return this.http.get<CategoryDto[]>(`${BASE}/MasterData/categories`);
  }

  getLocations(): Observable<LocationDto[]> {
    return this.http.get<LocationDto[]>(`${BASE}/MasterData/locations`);
  }

  getUnitsOfMeasure(): Observable<UnitOfMeasureDto[]> {
    return this.http.get<UnitOfMeasureDto[]>(`${BASE}/MasterData/units-of-measure`);
  }
}
