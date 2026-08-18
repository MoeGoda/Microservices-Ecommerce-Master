import { Component, inject, signal } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';
import { NAV_ENTRIES } from '../../../core/layout/nav-config';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';

interface BreadcrumbSegment {
  labelKey: string;
  route: string | null;
}

// N — the trail the reference demo shows on every non-home page
// ("Home > Product > <item>"). Derived entirely from NAV_ENTRIES
// (core/layout/nav-config.ts) rather than new per-screen data — every
// group/child route/label it needs is already there. Only renders for
// a route that's a group's child (Warehouse/Purchasing/Reports); a flat
// top-level link (POS, Users) or an unlisted route (login) has nothing
// to show a trail *to* beyond the page's own title, so it renders
// nothing rather than a redundant single crumb.
@Component({
  selector: 'app-breadcrumb',
  imports: [TranslatePipe],
  templateUrl: './breadcrumb.component.html',
  styleUrl: './breadcrumb.component.scss',
})
export class BreadcrumbComponent {
  private readonly router = inject(Router);

  readonly segments = signal<BreadcrumbSegment[]>([]);

  constructor() {
    this.updateSegments(this.router.url);
    this.router.events.pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd)).subscribe((e) => {
      this.updateSegments(e.urlAfterRedirects);
    });
  }

  private updateSegments(url: string): void {
    const path = url.split('?')[0].split('#')[0];

    for (const entry of NAV_ENTRIES) {
      if (entry.kind !== 'group') {
        continue;
      }

      for (const child of entry.children) {
        const matches = child.exact ? path === child.route : path === child.route || path.startsWith(child.route + '/');
        if (matches) {
          this.segments.set([
            { labelKey: entry.labelKey, route: null },
            { labelKey: child.labelKey, route: child.route },
          ]);
          return;
        }
      }
    }

    this.segments.set([]);
  }
}
