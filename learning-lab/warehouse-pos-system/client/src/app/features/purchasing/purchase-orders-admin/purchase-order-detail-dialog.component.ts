import { DecimalPipe } from '@angular/common';
import { Component, Inject, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { finalize, forkJoin } from 'rxjs';
import { I18nService } from '../../../core/i18n/i18n.service';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { NotificationService } from '../../../core/notifications/notification.service';
import { PurchaseOrderDetailDto, PurchaseOrderLineDto } from '../../../shared/models/purchasing.models';
import { LocationDto } from '../../../shared/models/warehouse.models';
import { WarehouseService } from '../../warehouse/warehouse.service';
import { PurchasingService } from '../purchasing.service';

export interface PurchaseOrderDetailDialogData {
  orderId: number;
}

// L — the "click a row's action button, see everything about it in a
// popup" pattern requested for this screen. Everything here (lines
// table, submit/cancel, per-line receive) is otherwise unchanged from
// the former inline detail panel — only its container moved from a
// mat-card on the page to a MatDialog. Always fetches its own copy on
// open rather than trusting the row's PurchaseOrderSummaryDto, the same
// "detail is its own fetch" reasoning the original selectOrder() used.
@Component({
  selector: 'app-purchase-order-detail-dialog',
  imports: [DecimalPipe, ReactiveFormsModule, MatButtonModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatProgressSpinnerModule, MatSelectModule, TranslatePipe],
  templateUrl: './purchase-order-detail-dialog.component.html',
  styleUrl: './purchase-order-detail-dialog.component.scss',
})
export class PurchaseOrderDetailDialogComponent implements OnInit {
  readonly order = signal<PurchaseOrderDetailDto | null>(null);
  readonly locations = signal<LocationDto[]>([]);
  readonly receivingLine = signal<PurchaseOrderLineDto | null>(null);

  readonly loadingDetail = signal(false);
  readonly submittingOrder = signal(false);
  readonly cancellingOrder = signal(false);
  readonly receivingStock = signal(false);

  readonly receiveForm = new FormGroup({
    locationId: new FormControl<number | null>(null, { validators: [Validators.required] }),
    quantity: new FormControl(1, { nonNullable: true, validators: [Validators.required, Validators.min(0.0001)] }),
    reference: new FormControl('', { nonNullable: true }),
  });

  constructor(
    @Inject(MAT_DIALOG_DATA) private readonly data: PurchaseOrderDetailDialogData,
    private readonly purchasingService: PurchasingService,
    private readonly warehouseService: WarehouseService,
    private readonly notification: NotificationService,
    private readonly i18n: I18nService,
  ) {}

  ngOnInit(): void {
    this.loadingDetail.set(true);
    forkJoin({
      order: this.purchasingService.getPurchaseOrder(this.data.orderId),
      locations: this.warehouseService.getLocations(),
    })
      .pipe(finalize(() => this.loadingDetail.set(false)))
      .subscribe(({ order, locations }) => {
        this.order.set(order);
        this.locations.set(locations);
      });
  }

  canSubmit(order: PurchaseOrderDetailDto): boolean {
    return order.status === 'Draft';
  }

  canCancel(order: PurchaseOrderDetailDto): boolean {
    return order.status === 'Draft' || order.status === 'Ordered';
  }

  canReceive(order: PurchaseOrderDetailDto): boolean {
    return order.status === 'Ordered' || order.status === 'PartiallyReceived';
  }

  remaining(line: PurchaseOrderLineDto): number {
    return line.orderedQuantity - line.receivedQuantity;
  }

  submitSubmit(): void {
    const order = this.order();
    if (!order || this.submittingOrder()) {
      return;
    }

    this.submittingOrder.set(true);
    this.purchasingService
      .submitPurchaseOrder(order.id)
      .pipe(finalize(() => this.submittingOrder.set(false)))
      .subscribe({
        next: (updated) => {
          this.notification.success(this.i18n.t('purchaseOrders.toasts.submitted', { orderNumber: updated.orderNumber }));
          this.order.set(updated);
        },
      });
  }

  submitCancel(): void {
    const order = this.order();
    if (!order || this.cancellingOrder()) {
      return;
    }

    this.cancellingOrder.set(true);
    this.purchasingService
      .cancelPurchaseOrder(order.id)
      .pipe(finalize(() => this.cancellingOrder.set(false)))
      .subscribe({
        next: (updated) => {
          this.notification.success(this.i18n.t('purchaseOrders.toasts.cancelled', { orderNumber: updated.orderNumber }));
          this.order.set(updated);
          this.receivingLine.set(null);
        },
      });
  }

  startReceive(line: PurchaseOrderLineDto): void {
    this.receivingLine.set(line);
    this.receiveForm.reset({ locationId: this.locations()[0]?.id ?? null, quantity: this.remaining(line), reference: '' });
  }

  cancelReceive(): void {
    this.receivingLine.set(null);
  }

  submitReceive(): void {
    const order = this.order();
    const line = this.receivingLine();
    if (!order || !line || this.receiveForm.invalid || this.receivingStock()) {
      this.receiveForm.markAllAsTouched();
      return;
    }

    const value = this.receiveForm.getRawValue();
    this.receivingStock.set(true);
    this.purchasingService
      .receiveLine(order.id, line.id, { locationId: value.locationId!, quantity: value.quantity, reference: value.reference })
      .pipe(finalize(() => this.receivingStock.set(false)))
      .subscribe({
        next: (updated) => {
          this.notification.success(
            this.i18n.t('purchaseOrders.toasts.received', {
              quantity: value.quantity,
              orderNumber: order.orderNumber,
              status: this.i18n.t('purchaseOrders.status.' + updated.status),
            }),
          );
          this.order.set(updated);
          this.receivingLine.set(null);
        },
      });
  }
}
