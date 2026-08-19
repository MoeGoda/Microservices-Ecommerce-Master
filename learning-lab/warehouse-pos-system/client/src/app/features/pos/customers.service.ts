import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../../shared/models/pagination.models';
import {
  AdjustCustomerBalanceRequest,
  CreateCustomerRequest,
  CustomerDto,
  UpdateCustomerRequest,
} from '../../shared/models/pos.models';

const BASE = `${environment.apiBaseUrl}/Pos/Customers`;

@Injectable({ providedIn: 'root' })
export class CustomersService {
  constructor(private readonly http: HttpClient) {}

  search(search: string | null, page = 1, pageSize = 20): Observable<PagedResult<CustomerDto>> {
    const params: Record<string, string | number> = { page, pageSize };
    if (search) {
      params['search'] = search;
    }
    return this.http.get<PagedResult<CustomerDto>>(BASE, { params });
  }

  getById(id: number): Observable<CustomerDto> {
    return this.http.get<CustomerDto>(`${BASE}/${id}`);
  }

  create(request: CreateCustomerRequest): Observable<CustomerDto> {
    return this.http.post<CustomerDto>(BASE, request);
  }

  update(id: number, request: UpdateCustomerRequest): Observable<CustomerDto> {
    return this.http.put<CustomerDto>(`${BASE}/${id}`, request);
  }

  adjustBalance(id: number, request: AdjustCustomerBalanceRequest): Observable<CustomerDto> {
    return this.http.post<CustomerDto>(`${BASE}/${id}/balance-adjustments`, request);
  }
}
