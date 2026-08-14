import { Component, effect, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
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

@Component({
  selector: 'app-root',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatToolbarModule,
    MatButtonModule,
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
}
