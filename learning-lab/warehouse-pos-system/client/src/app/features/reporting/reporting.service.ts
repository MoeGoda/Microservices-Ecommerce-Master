import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../../shared/models/pagination.models';
import {
  CashierPerformanceDto,
  SalesByDayDto,
  SalesLedgerEntryDto,
  StockLevelRecordDto,
  StockMovementRecordDto,
  TopSellingItemDto,
} from '../../shared/models/reporting.models';

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

  getSalesLedger(page = 1, pageSize = 20, fromUtc?: string, toUtc?: string): Observable<PagedResult<SalesLedgerEntryDto>> {
    return this.http.get<PagedResult<SalesLedgerEntryDto>>(`${BASE}/reports/sales-ledger`, {
      params: this.dateRangeParams(page, pageSize, fromUtc, toUtc),
    });
  }

  getCashierPerformance(fromUtc?: string, toUtc?: string): Observable<CashierPerformanceDto[]> {
    return this.http.get<CashierPerformanceDto[]>(`${BASE}/reports/cashier-performance`, {
      params: this.dateRangeParams(undefined, undefined, fromUtc, toUtc),
    });
  }

  getStockMovements(page = 1, pageSize = 20, fromUtc?: string, toUtc?: string): Observable<PagedResult<StockMovementRecordDto>> {
    return this.http.get<PagedResult<StockMovementRecordDto>>(`${BASE}/reports/stock-movements`, {
      params: this.dateRangeParams(page, pageSize, fromUtc, toUtc),
    });
  }

  private dateRangeParams(page?: number, pageSize?: number, fromUtc?: string, toUtc?: string): HttpParams {
    let params = new HttpParams();
    if (page !== undefined) {
      params = params.set('page', page);
    }
    if (pageSize !== undefined) {
      params = params.set('pageSize', pageSize);
    }
    if (fromUtc) {
      params = params.set('fromUtc', fromUtc);
    }
    if (toUtc) {
      params = params.set('toUtc', toUtc);
    }
    return params;
  }
}
