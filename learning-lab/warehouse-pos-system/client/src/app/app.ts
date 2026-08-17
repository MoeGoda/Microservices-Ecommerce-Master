import { Component, computed, effect, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatBadgeModule } from '@angular/material/badge';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { BreakpointObserver } from '@angular/cdk/layout';
import { AuthService } from './core/auth/auth.service';
import { NotificationFeedService } from './core/notification-feed/notification-feed.service';
import { NotificationDto } from './shared/models/notification.models';
import { ADMIN_ROLES, POS_ROLES, REPORTS_ROLES, USER_MANAGEMENT_ROLES } from './shared/models/roles';
import { TranslatePipe } from './core/i18n/translate.pipe';
import { LanguageSwitcherComponent } from './shared/components/language-switcher/language-switcher.component';

// K — a per-type icon for the notification feed dropdown, the "Facebook
// list" look the redesign asked for: an unread dot + an icon that hints
// at what kind of event this is, not just a wall of identical rows.
// Mirrors Notifications.Domain.Entities.NotificationType's string values
// (E1) — a type this map doesn't recognize falls back to a generic bell
// rather than showing nothing.
const NOTIFICATION_ICONS: Record<string, string> = {
  SaleCompleted: 'point_of_sale',
  LowStock: 'inventory_2',
};

export function notificationIcon(type: string): string {
  return NOTIFICATION_ICONS[type] ?? 'notifications';
}

@Component({
  selector: 'app-root',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatToolbarModule,
    MatButtonModule,
    MatDividerModule,
    MatIconModule,
    MatMenuModule,
    MatBadgeModule,
    MatSidenavModule,
    MatListModule,
    TranslatePipe,
    LanguageSwitcherComponent,
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  // Narrow screens get an overlay sidenav that starts closed (mode="over"
  // — a fixed "side" nav would otherwise permanently eat ~240px of a
  // phone-width viewport); wide screens keep the always-visible "side"
  // mode this shell was built around. `sidenavMode`/`sidenavOpened` drive
  // the two mat-sidenav attributes directly from the same breakpoint.
  readonly sidenavMode = signal<'side' | 'over'>('side');
  readonly sidenavOpened = signal(true);

  // K — the sidenav's one genuinely multi-screen group (Items/Suppliers/
  // Purchase Orders). POS/Reports/Users each stay a single flat link —
  // grouping a section with exactly one destination would just add a
  // click for no organizational benefit. Opens by default so a reload
  // while already on one of its own pages doesn't look collapsed/broken;
  // the constructor below re-opens it on navigation into the group too.
  readonly warehouseGroupOpen = signal(true);

  readonly userInitial = computed(() => {
    const name = this.authService.currentUser()?.userName;
    return name ? name.charAt(0).toUpperCase() : '?';
  });

  readonly notificationIcon = notificationIcon;

  constructor(
    readonly authService: AuthService,
    readonly notificationFeed: NotificationFeedService,
    private readonly router: Router,
    breakpointObserver: BreakpointObserver,
  ) {
    breakpointObserver.observe('(max-width: 768px)').subscribe((state) => {
      this.sidenavMode.set(state.matches ? 'over' : 'side');
      this.sidenavOpened.set(!state.matches);
    });

    // Covers both a fresh sign-in (LoginComponent sets currentUser, this
    // fires right after) and a page reload with an already-valid session
    // in localStorage (currentUser is set synchronously from storage at
    // AuthService construction, before this effect ever runs — so the
    // very first run already sees it). Logging out sets currentUser back
    // to null, which is exactly when the feed should disconnect and
    // forget what it knew.
    effect(() => {
      if (this.authService.currentUser()) {
        this.notificationFeed.loadRecent();
        this.notificationFeed.connect();
      } else {
        this.notificationFeed.disconnect();
      }
    });
  }

  // Mirrors roleGuard's own check — the toolbar shouldn't even offer a
  // link the user's role can't actually use, same "don't show a door
  // that leads to a 403" reasoning as the route guard itself.
  canSeeAdmin(): boolean {
    return this.hasAnyRole(ADMIN_ROLES);
  }

  canSeePos(): boolean {
    return this.hasAnyRole(POS_ROLES);
  }

  canSeeReports(): boolean {
    return this.hasAnyRole(REPORTS_ROLES);
  }

  canSeeUsers(): boolean {
    return this.hasAnyRole(USER_MANAGEMENT_ROLES);
  }

  private hasAnyRole(roles: readonly string[]): boolean {
    const user = this.authService.currentUser();
    return !!user && roles.includes(user.role);
  }

  logout(): void {
    this.authService.logout();
    this.router.navigateByUrl('/login');
  }

  markRead(notification: NotificationDto): void {
    if (notification.isRead) {
      return;
    }

    this.notificationFeed.markAsRead(notification.id).subscribe({
      next: (updated) => {
        this.notificationFeed.notifications.update((list) => list.map((n) => (n.id === updated.id ? updated : n)));
      },
    });
  }

  markAllRead(): void {
    this.notificationFeed.markAllAsRead().subscribe({
      next: () => {
        this.notificationFeed.notifications.update((list) => list.map((n) => ({ ...n, isRead: true })));
      },
    });
  }

  // Intl.RelativeTimeFormat picks its own wording ("5 minutes ago" /
  // "منذ 5 دقائق") from the browser's active locale — no custom en/ar
  // translation keys needed for this, unlike everything else in the app,
  // since this IS what the API is for.
  relativeTime(isoDate: string): string {
    const diffMs = new Date(isoDate).getTime() - Date.now();
    const diffMinutes = Math.round(diffMs / 60_000);
    const formatter = new Intl.RelativeTimeFormat(undefined, { numeric: 'auto' });

    if (Math.abs(diffMinutes) < 60) {
      return formatter.format(diffMinutes, 'minute');
    }

    const diffHours = Math.round(diffMinutes / 60);
    if (Math.abs(diffHours) < 24) {
      return formatter.format(diffHours, 'hour');
    }

    return formatter.format(Math.round(diffHours / 24), 'day');
  }
}
