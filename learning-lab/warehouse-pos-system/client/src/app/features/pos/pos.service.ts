import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AddSaleLineRequest,
  CashDrawerSessionDto,
  CashDrawerXReportDto,
  CashMovementDto,
  SaleDto,
  StartSaleRequest,
} from '../../shared/models/pos.models';

const BASE = `${environment.apiBaseUrl}/Pos`;

// Every call goes through the gateway's /Pos/... upstream routes, same as
// WarehouseService going through /Warehouse/... — never straight to
// POS.API's own port. authInterceptor already attaches the cashier's own
// token to every outgoing request; this service doesn't touch auth at all.
@Injectable({ providedIn: 'root' })
export class PosService {
  constructor(private readonly http: HttpClient) {}

  startSale(request: StartSaleRequest): Observable<SaleDto> {
    return this.http.post<SaleDto>(`${BASE}/Sales`, request);
  }

  getSale(saleId: number): Observable<SaleDto> {
    return this.http.get<SaleDto>(`${BASE}/Sales/${saleId}`);
  }

  // The "held sales" list — every InProgress sale, optionally narrowed to
  // one register's own locationId.
  getInProgressSales(locationId?: number | null): Observable<SaleDto[]> {
    const params = locationId != null ? { locationId } : undefined;
    return this.http.get<SaleDto[]>(`${BASE}/Sales`, { params });
  }

  addLine(saleId: number, request: AddSaleLineRequest): Observable<SaleDto> {
    return this.http.post<SaleDto>(`${BASE}/Sales/${saleId}/lines`, request);
  }

  removeLine(saleId: number, lineId: number): Observable<SaleDto> {
    return this.http.delete<SaleDto>(`${BASE}/Sales/${saleId}/lines/${lineId}`);
  }

  setLineDiscount(saleId: number, lineId: number, manualDiscountPercent: number | null): Observable<SaleDto> {
    return this.http.put<SaleDto>(`${BASE}/Sales/${saleId}/lines/${lineId}/discount`, { manualDiscountPercent });
  }

  setCustomer(saleId: number, customerId: number | null): Observable<SaleDto> {
    return this.http.put<SaleDto>(`${BASE}/Sales/${saleId}/customer`, { customerId });
  }

  setReceiptDiscount(saleId: number, manualReceiptDiscountPercent: number | null): Observable<SaleDto> {
    return this.http.put<SaleDto>(`${BASE}/Sales/${saleId}/receipt-discount`, { manualReceiptDiscountPercent });
  }

  setTaxExempt(saleId: number, isTaxExempt: boolean): Observable<SaleDto> {
    return this.http.put<SaleDto>(`${BASE}/Sales/${saleId}/tax-exempt`, { isTaxExempt });
  }

  checkout(saleId: number): Observable<SaleDto> {
    return this.http.post<SaleDto>(`${BASE}/Sales/${saleId}/checkout`, {});
  }

  cancelSale(saleId: number): Observable<SaleDto> {
    return this.http.post<SaleDto>(`${BASE}/Sales/${saleId}/cancel`, {});
  }

  returnSale(saleId: number): Observable<SaleDto> {
    return this.http.post<SaleDto>(`${BASE}/Sales/${saleId}/return`, {});
  }

  openCashDrawer(locationId: number, openingFloat: number): Observable<CashDrawerSessionDto> {
    return this.http.post<CashDrawerSessionDto>(`${BASE}/CashDrawer/open`, { locationId, openingFloat });
  }

  recordCashMovement(locationId: number, type: 'CashIn' | 'CashOut', amount: number, reason: string): Observable<CashMovementDto> {
    return this.http.post<CashMovementDto>(`${BASE}/CashDrawer/movements`, { locationId, type, amount, reason });
  }

  getCashDrawerXReport(sessionId: number): Observable<CashDrawerXReportDto> {
    return this.http.get<CashDrawerXReportDto>(`${BASE}/CashDrawer/${sessionId}/x-report`);
  }

  closeCashDrawer(sessionId: number, closingCount: number): Observable<CashDrawerSessionDto> {
    return this.http.post<CashDrawerSessionDto>(`${BASE}/CashDrawer/${sessionId}/close`, { closingCount });
  }
}
