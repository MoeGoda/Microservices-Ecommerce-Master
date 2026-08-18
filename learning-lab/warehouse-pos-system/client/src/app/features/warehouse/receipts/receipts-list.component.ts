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
import { ReceiveStockDialogComponent } from './receive-stock-dialog.component';

const RECEIPT_REASONS = new Set(['Received', 'PurchaseOrderReceived']);

// M — "Receipts" as its own screen: a filtered view over Reporting's
// stock-movements ledger (GetStockMovementsQuery — date-range/item/
// location filterable server-side, no reason filter server-side), so
// the reason narrowing and any text search happen client-side over the
// fetched page (see paginateClientSide). The two reasons shown here are
// the app's two real receiving flows: WarehouseService.receiveStock()
// (free-text restock, reason "Received") and a Purchase Order's
// per-line receive (reason "PurchaseOrderReceived", still driven from
// the Purchase Orders detail dialog — this screen only shows it as
// history, it doesn't duplicate that action).
@Component({
  selector: 'app-receipts-list',
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
  templateUrl: './receipts-list.component.html',
  styleUrl: './receipts-list.component.scss',
})
export class ReceiptsListComponent implements OnInit {
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
        this.allMovements = result.items.filter((m) => RECEIPT_REASONS.has(m.reason));
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

  reasonLabel(reason: string): string {
    return this.i18n.t('stockMovements.reason.' + reason);
  }

  openCreateDialog(): void {
    this.dialog
      .open(ReceiveStockDialogComponent)
      .afterClosed()
      .subscribe((created) => {
        if (created) {
          this.load();
        }
      });
  }

  openDetailDialog(movement: StockMovementRecordDto): void {
    const data: StockMovementDetailDialogData = { movement, reasonLabel: this.reasonLabel(movement.reason) };
    this.dialog.open(StockMovementDetailDialogComponent, { data });
  }
}
