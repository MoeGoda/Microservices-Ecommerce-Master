import { Injectable, computed, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import * as signalR from '@microsoft/signalr';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { NotificationDto } from '../../shared/models/notification.models';
import { AuthService } from '../auth/auth.service';
import { NotificationService } from '../notifications/notification.service';

const BASE = `${environment.apiBaseUrl}/Notifications`;

// The live, persisted notification feed (E1) — a bell dropdown backed by
// GetRecent + a SignalR push, NOT to be confused with
// core/notifications/NotificationService, the transient MatSnackBar toast
// wrapper every feature already uses for its own success/error messages.
// That one has no state and no memory; this one does (the signals below),
// and also fires a toast through that same NotificationService when a
// push arrives, so a live event is both remembered AND immediately seen —
// two different jobs, deliberately two different services.
@Injectable({ providedIn: 'root' })
export class NotificationFeedService {
  private hubConnection: signalR.HubConnection | null = null;

  readonly notifications = signal<NotificationDto[]>([]);
  readonly unreadCount = computed(() => this.notifications().filter((n) => !n.isRead).length);

  constructor(
    private readonly http: HttpClient,
    private readonly authService: AuthService,
    private readonly toast: NotificationService,
  ) {}

  loadRecent(take = 20): void {
    this.http
      .get<NotificationDto[]>(BASE, { params: new HttpParams().set('take', take) })
      .subscribe({ next: (list) => this.notifications.set(list) });
  }

  markAsRead(id: number): Observable<NotificationDto> {
    return this.http.post<NotificationDto>(`${BASE}/${id}/read`, null);
  }

  markAllAsRead(): Observable<number> {
    return this.http.post<number>(`${BASE}/read-all`, null);
  }

  // Called once from AppComponent when a user signs in (and whenever a
  // page loads with a still-valid session already in localStorage) —
  // never from a route guard or interceptor, since a dropped/failed
  // connection here must never block navigation the way a failed HTTP
  // call to a *required* resource would.
  connect(): void {
    if (this.hubConnection) {
      return;
    }

    // accessTokenFactory, not a static token — the client re-reads
    // AuthService's current token on every negotiate/reconnect attempt,
    // the same "always ask, never cache" discipline authInterceptor
    // already applies to every plain HTTP call.
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.notificationsHubUrl}/hubs/notifications`, {
        accessTokenFactory: () => this.authService.getToken() ?? '',
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('ReceiveNotification', (dto: NotificationDto) => {
      this.notifications.update((list) => [dto, ...list]);
      this.toast.success(dto.message);
    });

    this.hubConnection.start().catch(() => {
      // A live-push connection failing to establish is degraded UX (no
      // real-time toasts until the next reconnect attempt succeeds), not
      // a broken app — loadRecent() already covers "what did I miss" on
      // its own, so there's nothing to surface to the user here.
    });
  }

  disconnect(): void {
    this.hubConnection?.stop();
    this.hubConnection = null;
    this.notifications.set([]);
  }
}
