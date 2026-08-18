import { Component, EventEmitter, Input, Output, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';

// M — no list screen had a search/filter panel before this. This owns
// only the shared chrome (card, Search/Reset row, optional collapse) —
// the actual filter fields are content-projected since they differ per
// screen (text search, status select, date range, ...), avoiding a
// generic "field descriptor" system that would be more machinery than
// any one screen needs.
@Component({
  selector: 'app-filter-panel',
  imports: [MatButtonModule, MatCardModule, MatIconModule, TranslatePipe],
  templateUrl: './filter-panel.component.html',
  styleUrl: './filter-panel.component.scss',
})
export class FilterPanelComponent {
  @Input() collapsible = false;
  @Output() search = new EventEmitter<void>();
  @Output() reset = new EventEmitter<void>();

  readonly expanded = signal(true);

  toggle(): void {
    this.expanded.update((current) => !current);
  }
}
