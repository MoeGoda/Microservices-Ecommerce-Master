import { Component, computed, effect, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatAutocompleteModule, MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
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
import { I18nService } from './core/i18n/i18n.service';
import { NAV_ENTRIES, NavEntry } from './core/layout/nav-config';
import { NotificationFeedService } from './core/notification-feed/notification-feed.service';
import { NotificationDto } from './shared/models/notification.models';
import { TranslatePipe } from './core/i18n/translate.pipe';
import { BreadcrumbComponent } from './shared/components/breadcrumb/breadcrumb.component';
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

// P — SB Admin 2's own notification dropdown colors each alert's icon
// circle by kind (bg-primary/bg-success/bg-warning) rather than one
// flat tone for every row; this is the closest real equivalent — the
// two notification types this app actually has, mapped to the two
// tones that read as "a sale happened" (success) vs. "something needs
// attention" (warning).
const NOTIFICATION_TONES: Record<string, 'success' | 'warning'> = {
  SaleCompleted: 'success',
  LowStock: 'warning',
};

export function notificationTone(type: string): 'success' | 'warning' {
  return NOTIFICATION_TONES[type] ?? 'warning';
}

interface SearchableNavItem {
  readonly labelKey: string;
  readonly route: string;
  readonly icon: string;
  readonly roles: readonly string[];
}

// P — SB Admin 2's topbar leads with a search box (`.navbar-search`);
// this app has no global entity-search backend, so rather than fake
// one, this is a real quick-nav search — flattened once from
// NAV_ENTRIES's groups/links, filtered by the *translated* label so it
// matches what's actually on screen in either language.
const SEARCHABLE_NAV_ITEMS: readonly SearchableNavItem[] = NAV_ENTRIES.flatMap((entry) =>
  entry.kind === 'group'
    ? entry.children.map((child) => ({ labelKey: child.labelKey, route: child.route, icon: child.icon, roles: entry.roles }))
    : [{ labelKey: entry.labelKey, route: entry.route, icon: entry.icon, roles: entry.roles }],
);

@Component({
  selector: 'app-root',
  imports: [
    ReactiveFormsModule,
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatAutocompleteModule,
    MatToolbarModule,
    MatButtonModule,
    MatDividerModule,
    MatIconModule,
    MatMenuModule,
    MatBadgeModule,
    MatSidenavModule,
    MatListModule,
    TranslatePipe,
    BreadcrumbComponent,
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

  // M — generalized from K's single hardcoded warehouseGroupOpen signal:
  // any number of NAV_ENTRIES groups can now exist, each tracked by id in
  // one Set. All three groups start open (same "don't look collapsed/
  // broken on reload" reasoning K used for the one group it had) — POS/
  // Users stay flat single links since they have only one destination
  // each, same as before.
  readonly navEntries: readonly NavEntry[] = NAV_ENTRIES;
  readonly openGroups = signal<ReadonlySet<string>>(new Set(['warehouse', 'purchasing', 'reports']));

  readonly userInitial = computed(() => {
    const name = this.authService.currentUser()?.userName;
    return name ? name.charAt(0).toUpperCase() : '?';
  });

  readonly notificationIcon = notificationIcon;
  readonly notificationTone = notificationTone;

  readonly navSearchControl = new FormControl('', { nonNullable: true });
  // computed() only tracks *signal* reads — a plain `.value` read on a
  // FormControl never triggers it to re-run, since valueChanges is an
  // Observable, not a signal. Bridging it through toSignal() is what
  // actually makes navSearchResults recompute as the user types.
  private readonly navSearchQuery = toSignal(this.navSearchControl.valueChanges, { initialValue: '' });
  readonly navSearchResults = computed(() => {
    const query = this.navSearchQuery().trim().toLowerCase();
    const user = this.authService.currentUser();
    const visible = SEARCHABLE_NAV_ITEMS.filter((item) => !!user && item.roles.includes(user.role));
    if (!query) {
      return visible;
    }
    return visible.filter((item) => this.i18n.t(item.labelKey).toLowerCase().includes(query));
  });

  constructor(
    readonly authService: AuthService,
    readonly notificationFeed: NotificationFeedService,
    private readonly i18n: I18nService,
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
  // that leads to a 403" reasoning as the route guard itself. Generalized
  // from four one-off canSeeX() methods (K) into one taking any
  // NAV_ENTRIES roles array, since groups are now data, not template.
  canSee(roles: readonly string[]): boolean {
    const user = this.authService.currentUser();
    return !!user && roles.includes(user.role);
  }

  onNavSearchSelect(event: MatAutocompleteSelectedEvent): void {
    const route = event.option.value as string;
    this.navSearchControl.setValue('');
    this.router.navigateByUrl(route);
  }

  isGroupOpen(id: string): boolean {
    return this.openGroups().has(id);
  }

  toggleGroup(id: string): void {
    this.openGroups.update((current) => {
      const next = new Set(current);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
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
