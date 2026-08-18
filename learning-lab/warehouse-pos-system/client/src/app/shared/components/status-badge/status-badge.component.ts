import { Component, Input } from '@angular/core';

// M — consolidates the ~6 independently-duplicated `.status-chip` blocks
// (suppliers/users boolean active-inactive, purchase orders' Draft/
// Ordered/PartiallyReceived/Received/Cancelled) into one five-tone
// system built on the same --mat-sys-* tokens each of those already
// used. `status` is the raw key used for tone lookup (e.g. "Draft",
// "active"); `label` is what's actually displayed and stays
// already-translated text supplied by the caller, same as PageHeaderComponent.
export type StatusTone = 'neutral' | 'info' | 'success' | 'warning' | 'danger';

const STATUS_TONE_MAP: Readonly<Record<string, StatusTone>> = {
  active: 'success',
  inactive: 'neutral',
  Draft: 'neutral',
  Pending: 'warning',
  Ordered: 'info',
  PartiallyReceived: 'warning',
  Received: 'success',
  Approved: 'success',
  Posted: 'success',
  Cancelled: 'danger',
};

@Component({
  selector: 'app-status-badge',
  templateUrl: './status-badge.component.html',
  styleUrl: './status-badge.component.scss',
})
export class StatusBadgeComponent {
  @Input({ required: true }) status!: string;
  @Input() label?: string;
  // Escape hatch for a status string this map doesn't recognize yet —
  // callers can still force a tone rather than silently getting neutral.
  @Input() tone?: StatusTone;

  resolvedTone(): StatusTone {
    return this.tone ?? STATUS_TONE_MAP[this.status] ?? 'neutral';
  }

  displayLabel(): string {
    return this.label ?? this.status;
  }
}
