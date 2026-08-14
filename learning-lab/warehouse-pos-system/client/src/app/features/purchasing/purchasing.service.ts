import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../../shared/models/pagination.models';
import {
  CreatePurchaseOrderRequest,
  CreateSupplierRequest,
  PurchaseOrderDetailDto,
  PurchaseOrderSummaryDto,
  ReceivePurchaseOrderLineRequest,
  SupplierDto,
} from '../../shared/models/purchasing.models';

const BASE = `${environment.apiBaseUrl}/Warehouse`;

// Same gateway-only, no-direct-service-call convention as WarehouseService.
@Injectable({ providedIn: 'root' })
export class PurchasingService {
  constructor(private readonly http: HttpClient) {}

  getSuppliers(page = 1, pageSize = 20): Observable<PagedResult<SupplierDto>> {
    return this.http.get<PagedResult<SupplierDto>>(`${BASE}/Suppliers`, { params: { page, pageSize } });
  }

  createSupplier(request: CreateSupplierRequest): Observable<SupplierDto> {
    return this.http.post<SupplierDto>(`${BASE}/Suppliers`, request);
  }

  setSupplierActive(supplierId: number, isActive: boolean): Observable<SupplierDto> {
    return this.http.post<SupplierDto>(`${BASE}/Suppliers/${supplierId}/active`, { isActive });
  }

  getPurchaseOrders(page = 1, pageSize = 20): Observable<PagedResult<PurchaseOrderSummaryDto>> {
    return this.http.get<PagedResult<PurchaseOrderSummaryDto>>(`${BASE}/PurchaseOrders`, { params: { page, pageSize } });
  }

  getPurchaseOrder(id: number): Observable<PurchaseOrderDetailDto> {
    return this.http.get<PurchaseOrderDetailDto>(`${BASE}/PurchaseOrders/${id}`);
  }

  createPurchaseOrder(request: CreatePurchaseOrderRequest): Observable<PurchaseOrderDetailDto> {
    return this.http.post<PurchaseOrderDetailDto>(`${BASE}/PurchaseOrders`, request);
  }

  submitPurchaseOrder(id: number): Observable<PurchaseOrderDetailDto> {
    return this.http.post<PurchaseOrderDetailDto>(`${BASE}/PurchaseOrders/${id}/submit`, null);
  }

  cancelPurchaseOrder(id: number): Observable<PurchaseOrderDetailDto> {
    return this.http.post<PurchaseOrderDetailDto>(`${BASE}/PurchaseOrders/${id}/cancel`, null);
  }

  receiveLine(orderId: number, lineId: number, request: ReceivePurchaseOrderLineRequest): Observable<PurchaseOrderDetailDto> {
    return this.http.post<PurchaseOrderDetailDto>(`${BASE}/PurchaseOrders/${orderId}/lines/${lineId}/receive`, request);
  }
}
