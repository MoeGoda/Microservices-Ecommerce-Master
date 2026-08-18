import { Component } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';

// M — "Stock Counts" (physical inventory / cycle count) has no backing
// entity, command, or query anywhere in the domain — confirmed during
// planning, and per the approved decision this stays a placeholder
// rather than inventing a new backend feature. The nav entry and route
// exist so the requested menu shape is fully visible; this screen is
// honest about there being nothing behind it yet.
@Component({
  selector: 'app-stock-counts-placeholder',
  imports: [MatCardModule, EmptyStateComponent, PageHeaderComponent, TranslatePipe],
  templateUrl: './stock-counts-placeholder.component.html',
  styleUrl: './stock-counts-placeholder.component.scss',
})
export class StockCountsPlaceholderComponent {}
