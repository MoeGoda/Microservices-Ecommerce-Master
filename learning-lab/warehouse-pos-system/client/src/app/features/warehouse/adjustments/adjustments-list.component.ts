import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { finalize } from 'rxjs';
import { I18nService } from '../../../core/i18n/i18n.service';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { FilterPanelComponent } from '../../../shared/components/filter-panel/filter-panel.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { SearchableSelectComponent } from '../../../shared/components/searchable-select/searchable-select.component';
import {
  StockMovementDetailDialogComponent,
  StockMovementDetailDialogData,
} from '../../../shared/components/stock-movement-detail-dialog/stock-movement-detail-dialog.component';
import { emptyPage, PagedResult } from '../../../shared/models/pagination.models';
import { StockMovementRecordDto } from '../../../shared/models/reporting.models';
import { LocationDto } from '../../../shared/models/warehouse.models';
import { paginateClientSide } from '../../../shared/utils/paginate-client-side';
import { ReportingService } from '../../reporting/reporting.service';
import { WarehouseService } from '../warehouse.service';
import { AdjustStockDialogComponent } from './adjust-stock-dialog.component';

// M — "Adjustments": the positive half of AdjustStockCommand's signed
// quantity change (e.g. a cycle-count correction that finds MORE stock
// than expected). The negative half is the Issues screen — same
// command, split by sign into two screens per the approved framing,
// since "Adjustments" and "Issues" read as different real-world actions
// even though they're one backend capability.
@Component({
  selector: 'app-adjustments-list',
  imports: [
    DatePipe,
    DecimalPipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatDatepickerModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    EmptyStateComponent,
    FilterPanelComponent,
    PageHeaderComponent,
    SearchableSelectComponent,
    TranslatePipe,
  ],
  templateUrl: './adjustments-list.component.html',
  styleUrl: './adjustments-list.component.scss',
})
export class AdjustmentsListComponent implements OnInit {
  readonly pagedMovements = signal<PagedResult<StockMovementRecordDto>>(emptyPage());
  readonly loading = signal(false);
  readonly locations = signal<LocationDto[]>([]);

  private allMovements: StockMovementRecordDto[] = [];

  readonly filterForm = new FormGroup({
    search: new FormControl('', { nonNullable: true }),
    locationId: new FormControl<number | null>(null),
    fromUtc: new FormControl<Date | null>(null),
    toUtc: new FormControl<Date | null>(null),
  });

  readonly locationLabel = (location: LocationDto): string => `${location.code} — ${location.name}`;
  readonly locationValue = (location: LocationDto): number => location.id;

  constructor(
    private readonly reportingService: ReportingService,
    private readonly warehouseService: WarehouseService,
    private readonly dialog: MatDialog,
    private readonly i18n: I18nService,
  ) {}

  ngOnInit(): void {
    this.warehouseService.getLocations().subscribe((locations) => this.locations.set(locations));
    this.load();
  }

  load(): void {
    this.loading.set(true);
    const value = this.filterForm.getRawValue();
    this.reportingService
      .getStockMovements(1, 100, value.fromUtc?.toISOString(), value.toUtc?.toISOString())
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((result) => {
        this.allMovements = result.items.filter((m) => m.reason === 'Adjustment' && m.quantityChange > 0);
        this.applyClientFilters(1);
      });
  }

  applyClientFilters(page: number): void {
    const value = this.filterForm.getRawValue();
    const search = value.search.trim().toLowerCase();
    const filtered = this.allMovements.filter(
      (m) =>
        (!search || m.sku.toLowerCase().includes(search) || m.itemName.toLowerCase().includes(search)) &&
        (!value.locationId || m.locationId === value.locationId),
    );
    this.pagedMovements.set(paginateClientSide(filtered, page, this.pagedMovements().pageSize));
  }

  onSearch(): void {
    this.load();
  }

  onResetFilters(): void {
    this.filterForm.reset({ search: '', locationId: null, fromUtc: null, toUtc: null });
    this.load();
  }

  onPageChange(event: PageEvent): void {
    if (event.pageSize !== this.pagedMovements().pageSize) {
      this.pagedMovements.update((current) => ({ ...current, pageSize: event.pageSize }));
    }
    this.applyClientFilters(event.pageIndex + 1);
  }

  reasonLabel(): string {
    return this.i18n.t('stockMovements.reason.Adjustment');
  }

  openCreateDialog(): void {
    this.dialog
      .open(AdjustStockDialogComponent)
      .afterClosed()
      .subscribe((created) => {
        if (created) {
          this.load();
        }
      });
  }

  openDetailDialog(movement: StockMovementRecordDto): void {
    const data: StockMovementDetailDialogData = { movement, reasonLabel: this.reasonLabel() };
    this.dialog.open(StockMovementDetailDialogComponent, { data });
  }
}
