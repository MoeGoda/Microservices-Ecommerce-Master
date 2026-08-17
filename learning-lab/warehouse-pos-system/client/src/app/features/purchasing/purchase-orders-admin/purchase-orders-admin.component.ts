import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize } from 'rxjs';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { emptyPage, PagedResult } from '../../../shared/models/pagination.models';
import { PurchaseOrderSummaryDto } from '../../../shared/models/purchasing.models';
import { PurchasingService } from '../purchasing.service';
import { PurchaseOrderCreateDialogComponent } from './purchase-order-create-dialog.component';
import { PurchaseOrderDetailDialogComponent } from './purchase-order-detail-dialog.component';

// L — the PO screen: create a Draft with its lines, submit it (locks the
// lines), and receive against it one line at a time — the create form
// and the detail/submit/cancel/receive panel both moved into dialogs
// (PurchaseOrderCreateDialogComponent / PurchaseOrderDetailDialogComponent),
// so this component only owns the paged list itself. Deliberately
// separate from item-detail's own receive form (ReceiveStockCommand,
// unchanged) — this is a distinct workflow with its own lifecycle, not a
// replacement for the free-text restock flow.
@Component({
  selector: 'app-purchase-orders-admin',
  imports: [DatePipe, DecimalPipe, MatButtonModule, MatCardModule, MatDialogModule, MatIconModule, MatPaginatorModule, MatProgressSpinnerModule, TranslatePipe],
  templateUrl: './purchase-orders-admin.component.html',
  styleUrl: './purchase-orders-admin.component.scss',
})
export class PurchaseOrdersAdminComponent implements OnInit {
  readonly pagedOrders = signal<PagedResult<PurchaseOrderSummaryDto>>(emptyPage());
  readonly loadingOrders = signal(false);

  constructor(
    private readonly purchasingService: PurchasingService,
    private readonly dialog: MatDialog,
  ) {}

  ngOnInit(): void {
    this.loadOrders();
  }

  loadOrders(page = 1): void {
    this.loadingOrders.set(true);
    this.purchasingService
      .getPurchaseOrders(page, this.pagedOrders().pageSize)
      .pipe(finalize(() => this.loadingOrders.set(false)))
      .subscribe({ next: (result) => this.pagedOrders.set(result) });
  }

  onOrdersPageChange(event: PageEvent): void {
    if (event.pageSize !== this.pagedOrders().pageSize) {
      this.pagedOrders.update((current) => ({ ...current, pageSize: event.pageSize }));
    }
    this.loadOrders(event.pageIndex + 1);
  }

  openCreateDialog(): void {
    this.dialog
      .open(PurchaseOrderCreateDialogComponent)
      .afterClosed()
      .subscribe((created) => {
        if (created) {
          this.loadOrders(this.pagedOrders().page);
        }
      });
  }

  openDetailDialog(order: PurchaseOrderSummaryDto): void {
    // Always reload on close, not just when something looks changed —
    // a dialog session can submit, cancel, AND receive one line all
    // before closing, so tracking "did anything change" isn't worth it
    // when a refetch of the current page is this cheap.
    this.dialog
      .open(PurchaseOrderDetailDialogComponent, { data: { orderId: order.id } })
      .afterClosed()
      .subscribe(() => this.loadOrders(this.pagedOrders().page));
  }
}
