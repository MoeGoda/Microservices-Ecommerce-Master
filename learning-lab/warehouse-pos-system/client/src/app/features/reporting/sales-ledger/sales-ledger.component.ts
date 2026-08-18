import { DatePipe, DecimalPipe } from '@angular/common';
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
import { SalesLedgerEntryDto } from '../../../shared/models/reporting.models';
import { UserDto } from '../../../shared/models/users.models';
import { UsersService } from '../../users/users.service';
import { ReportingService } from '../reporting.service';

// N-C — split out of the former ReportsDashboardComponent. The shared
// date form the monolith used for three reports at once is now this
// screen's own <app-filter-panel> + mat-datepicker, matching the pattern
// Warehouse's Receipts/Transfers/Adjustments/Issues already use (M) —
// a Date|null FormControl converted with .toISOString(), not the
// monolith's native <input type="date"> string.
@Component({
  selector: 'app-sales-ledger',
  imports: [
    DatePipe,
    DecimalPipe,
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
  templateUrl: './sales-ledger.component.html',
  styleUrl: './sales-ledger.component.scss',
})
export class SalesLedgerComponent implements OnInit {
  readonly loading = signal(false);
  readonly salesLedger = signal<PagedResult<SalesLedgerEntryDto>>(emptyPage());
  readonly cashierNames = signal<Map<number, string>>(new Map());

  readonly filterForm = new FormGroup({
    fromUtc: new FormControl<Date | null>(null),
    toUtc: new FormControl<Date | null>(null),
  });

  constructor(
    private readonly reportingService: ReportingService,
    private readonly usersService: UsersService,
  ) {}

  ngOnInit(): void {
    // UsersController is Admin-only (H), but this screen is also open to
    // Manager (REPORTS_ROLES) — a Manager viewing this report simply
    // falls back to "User #N" rather than the whole screen failing to
    // load over one 403 on a name lookup that isn't essential.
    this.usersService.getUsers(1, 100).subscribe({
      next: (result) => this.cashierNames.set(new Map(result.items.map((u: UserDto) => [u.id, u.userName]))),
      error: () => this.cashierNames.set(new Map()),
    });
    this.load(1);
  }

  private load(page: number): void {
    const { fromUtc, toUtc } = this.filterForm.getRawValue();
    this.loading.set(true);
    this.reportingService
      .getSalesLedger(page, this.salesLedger().pageSize, fromUtc?.toISOString(), toUtc?.toISOString())
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((result) => this.salesLedger.set(result));
  }

  onSearch(): void {
    this.load(1);
  }

  onResetFilters(): void {
    this.filterForm.reset({ fromUtc: null, toUtc: null });
    this.load(1);
  }

  onPageChange(event: PageEvent): void {
    if (event.pageSize !== this.salesLedger().pageSize) {
      this.salesLedger.update((current) => ({ ...current, pageSize: event.pageSize }));
    }
    this.load(event.pageIndex + 1);
  }

  cashierName(cashierUserId: number): string {
    return this.cashierNames().get(cashierUserId) ?? `#${cashierUserId}`;
  }
}
