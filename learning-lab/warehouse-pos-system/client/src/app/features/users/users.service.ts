import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../../shared/models/pagination.models';
import { CreateUserRequest, UserDto } from '../../shared/models/users.models';

const BASE = `${environment.apiBaseUrl}/Identity`;

// Every call goes through the gateway's /Identity/... upstream routes, same
// as AuthService. createUser deliberately calls Auth/create-user — the F2
// endpoint that already exists and already carries [Authorize(Roles=Admin)]
// — rather than adding a duplicate create action under UsersController.
@Injectable({ providedIn: 'root' })
export class UsersService {
  constructor(private readonly http: HttpClient) {}

  getUsers(page = 1, pageSize = 20): Observable<PagedResult<UserDto>> {
    return this.http.get<PagedResult<UserDto>>(`${BASE}/Users`, { params: { page, pageSize } });
  }

  createUser(request: CreateUserRequest): Observable<unknown> {
    return this.http.post(`${BASE}/Auth/create-user`, request);
  }

  setActive(userId: number, isActive: boolean): Observable<UserDto> {
    return this.http.post<UserDto>(`${BASE}/Users/${userId}/active`, { isActive });
  }
}
