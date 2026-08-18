import { Component, EventEmitter, Input, Output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

// M — the global `.empty-state` class (icon + centered text, defined in
// styles.scss) existed but nothing consumed it as a component; every
// screen either skipped an empty state or wrote its own `.empty-row`
// text cell. This wraps that existing global class in a reusable
// component with an optional action button, for use both inside a
// table's `@empty` block and as a standalone placeholder (e.g. the
// Stock Counts "not available yet" screen).
@Component({
  selector: 'app-empty-state',
  imports: [MatButtonModule, MatIconModule],
  templateUrl: './empty-state.component.html',
  styleUrl: './empty-state.component.scss',
})
export class EmptyStateComponent {
  @Input() icon = 'inbox';
  @Input({ required: true }) message!: string;
  @Input() actionLabel?: string;
  @Output() readonly action = new EventEmitter<void>();
}
