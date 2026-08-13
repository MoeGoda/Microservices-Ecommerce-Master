import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { SalesByDayDto, StockLevelRecordDto, TopSellingItemDto } from '../../shared/models/reporting.models';

const BASE = `${environment.apiBaseUrl}/Reporting`;

// Every call goes through the gateway's /Reporting/reports/... upstream
// routes (ocelot.json, D2) — same pattern as WarehouseService. Reporting's
// ingestion endpoints (EventsController) have no gateway route at all and
// no place in this service; they're service-to-service only.
@Injectable({ providedIn: 'root' })
export class ReportingService {
  constructor(private readonly http: HttpClient) {}

  getSalesByDay(): Observable<SalesByDayDto[]> {
    return this.http.get<SalesByDayDto[]>(`${BASE}/reports/sales-by-day`);
  }

  getTopSellingItems(take = 10): Observable<TopSellingItemDto[]> {
    return this.http.get<TopSellingItemDto[]>(`${BASE}/reports/top-selling-items`, {
      params: new HttpParams().set('take', take),
    });
  }

  getLowStock(): Observable<StockLevelRecordDto[]> {
    return this.http.get<StockLevelRecordDto[]>(`${BASE}/reports/low-stock`);
  }
}
