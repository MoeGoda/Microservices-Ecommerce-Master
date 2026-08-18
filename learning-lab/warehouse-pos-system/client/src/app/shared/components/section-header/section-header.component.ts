import { Component, Input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

// N — the bold, icon-led, solid-color card header the reference demo
// used on every one of its home-page cards ("Last products", "Best
// products", "Top product"): only worth using where a screen actually
// shows several side-by-side cards that each need their own identity —
// a dashboard. Single-card list/detail screens already carry their
// title via <app-page-header> above the card; adding this there would
// just duplicate it. See WarehouseDashboardComponent / the new
// ReportsDashboardComponent for the intended usage.
export type SectionHeaderTone = 'primary' | 'success' | 'info' | 'warning' | 'danger';

@Component({
  selector: 'app-section-header',
  imports: [MatIconModule],
  templateUrl: './section-header.component.html',
  styleUrl: './section-header.component.scss',
})
export class SectionHeaderComponent {
  @Input() icon = 'dashboard';
  @Input({ required: true }) title!: string;
  @Input() tone: SectionHeaderTone = 'primary';
}
