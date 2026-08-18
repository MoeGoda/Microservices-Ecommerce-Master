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
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { finalize } from 'rxjs';
import { I18nService } from '../../../core/i18n/i18n.service';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { FilterPanelComponent } from '../../../shared/components/filter-panel/filter-panel.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { SearchableSelectComponent } from '../../../shared/components/searchable-select/searchable-select.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { emptyPage, PagedResult } from '../../../shared/models/pagination.models';
import { PurchaseOrderStatus, PurchaseOrderSummaryDto, SupplierDto } from '../../../shared/models/purchasing.models';
import { paginateClientSide } from '../../../shared/utils/paginate-client-side';
import { PurchasingService } from '../purchasing.service';
import { PurchaseOrderCreateDialogComponent } from './purchase-order-create-dialog.component';
import { PurchaseOrderDetailDialogComponent } from './purchase-order-detail-dialog.component';

const STATUSES: PurchaseOrderStatus[] = ['Draft', 'Ordered', 'PartiallyReceived', 'Received', 'Cancelled'];

// L — the PO screen: create a Draft with its lines, submit it (locks the
// lines), and receive against it one line at a time — the create form
// and the detail/submit/cancel/receive panel both moved into dialogs
// (PurchaseOrderCreateDialogComponent / PurchaseOrderDetailDialogComponent),
// so this component only owns the paged list itself. Deliberately
// separate from item-detail's own receive form (ReceiveStockCommand,
// unchanged) — this is a distinct workflow with its own lifecycle, not a
// replacement for the free-text restock flow.
// M — GetPurchaseOrdersQuery has no server-side search/filter (only
// page/pageSize, capped at 100) — same client-side filter/paginate
// pattern as Suppliers/Items/the new Warehouse ledger screens.
@Component({
  selector: 'app-purchase-orders-admin',
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
    MatSelectModule,
    MatTooltipModule,
    EmptyStateComponent,
    FilterPanelComponent,
    PageHeaderComponent,
    SearchableSelectComponent,
    StatusBadgeComponent,
    TranslatePipe,
  ],
  templateUrl: './purchase-orders-admin.component.html',
  styleUrl: './purchase-orders-admin.component.scss',
})
export class PurchaseOrdersAdminComponent implements OnInit {
  readonly pagedOrders = signal<PagedResult<PurchaseOrderSummaryDto>>(emptyPage());
  readonly loadingOrders = signal(false);
  readonly suppliers = signal<SupplierDto[]>([]);
  readonly statuses = STATUSES;

  private allOrders: PurchaseOrderSummaryDto[] = [];

  readonly filterForm = new FormGroup({
    search: new FormControl('', { nonNullable: true }),
    supplierId: new FormControl<number | null>(null),
    status: new FormControl<PurchaseOrderStatus | null>(null),
    fromUtc: new FormControl<Date | null>(null),
    toUtc: new FormControl<Date | null>(null),
  });

  readonly supplierLabel = (supplier: SupplierDto): string => supplier.name;
  readonly supplierValue = (supplier: SupplierDto): number => supplier.id;

  constructor(
    private readonly purchasingService: PurchasingService,
    private readonly dialog: MatDialog,
    private readonly i18n: I18nService,
  ) {}

  ngOnInit(): void {
    this.purchasingService.getSuppliers(1, 100).subscribe((result) => this.suppliers.set(result.items));
    this.loadOrders();
  }

  loadOrders(keepPage = false): void {
    this.loadingOrders.set(true);
    const currentPage = this.pagedOrders().page;
    this.purchasingService
      .getPurchaseOrders(1, 100)
      .pipe(finalize(() => this.loadingOrders.set(false)))
      .subscribe({
        next: (result) => {
          this.allOrders = result.items;
          this.applyClientFilters(keepPage ? currentPage : 1);
        },
      });
  }

  applyClientFilters(page: number): void {
    const value = this.filterForm.getRawValue();
    const search = value.search.trim().toLowerCase();
    const fromMs = value.fromUtc?.getTime();
    const toMs = value.toUtc?.getTime();
    const filtered = this.allOrders.filter((order) => {
      const createdMs = new Date(order.createdAt).getTime();
      return (
        (!search || order.orderNumber.toLowerCase().includes(search) || order.supplierName.toLowerCase().includes(search)) &&
        (!value.supplierId || order.supplierId === value.supplierId) &&
        (!value.status || order.status === value.status) &&
        (fromMs == null || createdMs >= fromMs) &&
        (toMs == null || createdMs <= toMs)
      );
    });
    this.pagedOrders.set(paginateClientSide(filtered, page, this.pagedOrders().pageSize));
  }

  onSearch(): void {
    this.applyClientFilters(1);
  }

  onResetFilters(): void {
    this.filterForm.reset({ search: '', supplierId: null, status: null, fromUtc: null, toUtc: null });
    this.applyClientFilters(1);
  }

  onOrdersPageChange(event: PageEvent): void {
    if (event.pageSize !== this.pagedOrders().pageSize) {
      this.pagedOrders.update((current) => ({ ...current, pageSize: event.pageSize }));
    }
    this.applyClientFilters(event.pageIndex + 1);
  }

  statusLabel(status: string): string {
    return this.i18n.t('purchaseOrders.status.' + status);
  }

  openCreateDialog(): void {
    this.dialog
      .open(PurchaseOrderCreateDialogComponent)
      .afterClosed()
      .subscribe((created) => {
        if (created) {
          this.loadOrders();
        }
      });
  }

  openDetailDialog(order: PurchaseOrderSummaryDto): void {
    // Always reload on close, not just when something looks changed —
    // a dialog session can submit, cancel, AND receive one line all
    // before closing, so tracking "did anything change" isn't worth it
    // when a refetch of the current batch is this cheap.
    this.dialog
      .open(PurchaseOrderDetailDialogComponent, { data: { orderId: order.id } })
      .afterClosed()
      .subscribe(() => this.loadOrders(true));
  }
}
