import { DatePipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize } from 'rxjs';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { FilterPanelComponent } from '../../../shared/components/filter-panel/filter-panel.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { emptyPage, PagedResult } from '../../../shared/models/pagination.models';
import { StockMovementRecordDto } from '../../../shared/models/reporting.models';
import { ReportingService } from '../reporting.service';

// N-C — split out of the former ReportsDashboardComponent, own date
// filter. Unlike Warehouse's Receipts/Transfers/Adjustments/Issues
// screens, this stays unfiltered by reason — it's the full audit trail
// across every stock-affecting reason, not one narrowed slice of it.
@Component({
  selector: 'app-stock-movements-report',
  imports: [
    DatePipe,
    ReactiveFormsModule,
    MatCardModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    FilterPanelComponent,
    PageHeaderComponent,
    TranslatePipe,
  ],
  templateUrl: './stock-movements-report.component.html',
  styleUrl: './stock-movements-report.component.scss',
})
export class StockMovementsReportComponent implements OnInit {
  readonly loading = signal(false);
  readonly stockMovements = signal<PagedResult<StockMovementRecordDto>>(emptyPage());

  readonly filterForm = new FormGroup({
    fromUtc: new FormControl<Date | null>(null),
    toUtc: new FormControl<Date | null>(null),
  });

  constructor(private readonly reportingService: ReportingService) {}

  ngOnInit(): void {
    this.load(1);
  }

  private load(page: number): void {
    const { fromUtc, toUtc } = this.filterForm.getRawValue();
    this.loading.set(true);
    this.reportingService
      .getStockMovements(page, this.stockMovements().pageSize, fromUtc?.toISOString(), toUtc?.toISOString())
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((result) => this.stockMovements.set(result));
  }

  onSearch(): void {
    this.load(1);
  }

  onResetFilters(): void {
    this.filterForm.reset({ fromUtc: null, toUtc: null });
    this.load(1);
  }

  onPageChange(event: PageEvent): void {
    if (event.pageSize !== this.stockMovements().pageSize) {
      this.stockMovements.update((current) => ({ ...current, pageSize: event.pageSize }));
    }
    this.load(event.pageIndex + 1);
  }
}
