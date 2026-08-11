import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, CurrentUser, LoginRequest } from '../../shared/models/auth.models';

const STORAGE_KEY = 'warehousepos.auth';

// Holds the signed-in user's state for the whole app and is the *only*
// place that reads/writes the stored token. Everything else — the login
// component, the auth guard, the HTTP interceptor — goes through this
// service rather than touching localStorage directly.
//
// localStorage (not an httpOnly cookie) is a deliberate, known tradeoff for
// this learning-lab stage: it's simple and framework-agnostic, but it's
// readable by any JavaScript running on the page, so a successful XSS
// attack anywhere in this app could steal the token. An httpOnly cookie set
// by the backend would close that hole at the cost of needing CSRF
// protection instead. Revisiting this is explicitly one of Phase F2's
// security-hardening items, not something to solve by accident here.
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly _currentUser = signal<CurrentUser | null>(this.readCurrentUserFromStorage());

  // Exposed as a read-only signal: components can react to sign-in/sign-out
  // (e.g. the toolbar showing/hiding the username) without polling or
  // subscribing to an Observable for what is simple, synchronous state.
  readonly currentUser = this._currentUser.asReadonly();

  constructor(private readonly http: HttpClient) {}

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${environment.apiBaseUrl}/Identity/Auth/login`, request)
      .pipe(tap((response) => this.startSession(response)));
  }

  logout(): void {
    localStorage.removeItem(STORAGE_KEY);
    this._currentUser.set(null);
  }

  getToken(): string | null {
    return this.readStoredAuth()?.token ?? null;
  }

  isAuthenticated(): boolean {
    const user = this._currentUser();
    if (!user) {
      return false;
    }
    // Client-side expiry check only short-circuits obviously-stale tokens
    // to avoid a doomed request — it is NOT what actually protects
    // anything. The JWT's signature and server-side expiry check (both the
    // gateway and Identity.API validate it independently) are what matter;
    // this is just a fast local guess to skip a request that would 401 anyway.
    return new Date(user.expiresAtUtc).getTime() > Date.now();
  }

  private startSession(response: AuthResponse): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(response));
    this._currentUser.set({
      userName: response.userName,
      role: response.role,
      expiresAtUtc: response.expiresAtUtc,
    });
  }

  private readStoredAuth(): AuthResponse | null {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return null;
    }
    try {
      return JSON.parse(raw) as AuthResponse;
    } catch {
      // Corrupt/tampered storage value — treat it as "not signed in" rather
      // than letting JSON.parse's exception propagate into app startup.
      return null;
    }
  }

  private readCurrentUserFromStorage(): CurrentUser | null {
    const stored = this.readStoredAuth();
    return stored
      ? { userName: stored.userName, role: stored.role, expiresAtUtc: stored.expiresAtUtc }
      : null;
  }
}
