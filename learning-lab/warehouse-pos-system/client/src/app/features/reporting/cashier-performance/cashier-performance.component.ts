import { DecimalPipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize } from 'rxjs';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { FilterPanelComponent } from '../../../shared/components/filter-panel/filter-panel.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { CashierPerformanceDto } from '../../../shared/models/reporting.models';
import { UserDto } from '../../../shared/models/users.models';
import { UsersService } from '../../users/users.service';
import { ReportingService } from '../reporting.service';

// N-C — split out of the former ReportsDashboardComponent, own date
// filter (same reasoning as SalesLedgerComponent).
@Component({
  selector: 'app-cashier-performance',
  imports: [
    DecimalPipe,
    ReactiveFormsModule,
    MatCardModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    FilterPanelComponent,
    PageHeaderComponent,
    TranslatePipe,
  ],
  templateUrl: './cashier-performance.component.html',
  styleUrl: './cashier-performance.component.scss',
})
export class CashierPerformanceComponent implements OnInit {
  readonly loading = signal(false);
  readonly cashierPerformance = signal<CashierPerformanceDto[]>([]);
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
    this.usersService.getUsers(1, 100).subscribe({
      next: (result) => this.cashierNames.set(new Map(result.items.map((u: UserDto) => [u.id, u.userName]))),
      error: () => this.cashierNames.set(new Map()),
    });
    this.load();
  }

  private load(): void {
    const { fromUtc, toUtc } = this.filterForm.getRawValue();
    this.loading.set(true);
    this.reportingService
      .getCashierPerformance(fromUtc?.toISOString(), toUtc?.toISOString())
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((rows) => this.cashierPerformance.set(rows));
  }

  onSearch(): void {
    this.load();
  }

  onResetFilters(): void {
    this.filterForm.reset({ fromUtc: null, toUtc: null });
    this.load();
  }

  cashierName(cashierUserId: number): string {
    return this.cashierNames().get(cashierUserId) ?? `#${cashierUserId}`;
  }
}
