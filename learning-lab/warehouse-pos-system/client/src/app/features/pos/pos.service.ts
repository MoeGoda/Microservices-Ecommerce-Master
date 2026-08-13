import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AddSaleLineRequest, SaleDto, StartSaleRequest } from '../../shared/models/pos.models';

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

  addLine(saleId: number, request: AddSaleLineRequest): Observable<SaleDto> {
    return this.http.post<SaleDto>(`${BASE}/Sales/${saleId}/lines`, request);
  }

  removeLine(saleId: number, lineId: number): Observable<SaleDto> {
    return this.http.delete<SaleDto>(`${BASE}/Sales/${saleId}/lines/${lineId}`);
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
}
