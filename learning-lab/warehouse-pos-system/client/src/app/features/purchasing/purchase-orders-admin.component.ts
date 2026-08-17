import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormArray, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { finalize, forkJoin } from 'rxjs';
import { I18nService } from '../../core/i18n/i18n.service';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { NotificationService } from '../../core/notifications/notification.service';
import { emptyPage, PagedResult } from '../../shared/models/pagination.models';
import {
  PurchaseOrderDetailDto,
  PurchaseOrderLineDto,
  PurchaseOrderSummaryDto,
  SupplierDto,
} from '../../shared/models/purchasing.models';
import { ItemSummaryDto, LocationDto, UnitOfMeasureDto } from '../../shared/models/warehouse.models';
import { WarehouseService } from '../warehouse/warehouse.service';
import { PurchasingService } from './purchasing.service';

// Named so addLine()/the `lines` FormArray getter give getRawValue() a
// concrete shape — an inline FormGroup literal infers an index-signature
// type that TypeScript's noPropertyAccessFromIndexSignature then forbids
// dotted access on (l.itemId, etc.) in submitCreate().
type PurchaseOrderLineFormGroup = FormGroup<{
  itemId: FormControl<number | null>;
  unitOfMeasureId: FormControl<number | null>;
  orderedQuantity: FormControl<number>;
  unitCost: FormControl<number>;
}>;

// I — the PO screen: create a Draft with its lines, submit it (locks the
// lines), and receive against it one line at a time. Deliberately
// separate from ItemsAdminComponent/items receiving (ReceiveStockCommand,
// unchanged) — this is a distinct workflow with its own lifecycle, not a
// replacement for the free-text restock flow.
@Component({
  selector: 'app-purchase-orders-admin',
  imports: [
    DatePipe,
    DecimalPipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    TranslatePipe,
  ],
  templateUrl: './purchase-orders-admin.component.html',
  styleUrl: './purchase-orders-admin.component.scss',
})
export class PurchaseOrdersAdminComponent implements OnInit {
  readonly suppliers = signal<SupplierDto[]>([]);
  readonly items = signal<ItemSummaryDto[]>([]);
  readonly units = signal<UnitOfMeasureDto[]>([]);
  readonly locations = signal<LocationDto[]>([]);

  readonly pagedOrders = signal<PagedResult<PurchaseOrderSummaryDto>>(emptyPage());
  readonly selectedOrder = signal<PurchaseOrderDetailDto | null>(null);
  readonly receivingLine = signal<PurchaseOrderLineDto | null>(null);

  readonly loadingOrders = signal(false);
  readonly loadingDetail = signal(false);
  readonly creatingOrder = signal(false);
  readonly submittingOrder = signal(false);
  readonly cancellingOrder = signal(false);
  readonly receivingStock = signal(false);

  readonly createForm = new FormGroup({
    supplierId: new FormControl<number | null>(null, { validators: [Validators.required] }),
    notes: new FormControl('', { nonNullable: true }),
    lines: new FormArray<PurchaseOrderLineFormGroup>([]),
  });

  readonly receiveForm = new FormGroup({
    locationId: new FormControl<number | null>(null, { validators: [Validators.required] }),
    quantity: new FormControl(1, { nonNullable: true, validators: [Validators.required, Validators.min(0.0001)] }),
    reference: new FormControl('', { nonNullable: true }),
  });

  get lines(): FormArray<PurchaseOrderLineFormGroup> {
    return this.createForm.controls.lines;
  }

  constructor(
    private readonly purchasingService: PurchasingService,
    private readonly warehouseService: WarehouseService,
    private readonly notification: NotificationService,
    private readonly i18n: I18nService,
  ) {}

  ngOnInit(): void {
    forkJoin({
      suppliers: this.purchasingService.getSuppliers(1, 100),
      items: this.warehouseService.getItems(1, 100),
      units: this.warehouseService.getUnitsOfMeasure(),
      locations: this.warehouseService.getLocations(),
    }).subscribe(({ suppliers, items, units, locations }) => {
      this.suppliers.set(suppliers.items);
      this.items.set(items.items);
      this.units.set(units);
      this.locations.set(locations);
    });

    this.addLine();
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

  addLine(): void {
    this.lines.push(
      new FormGroup({
        itemId: new FormControl<number | null>(null, { validators: [Validators.required] }),
        unitOfMeasureId: new FormControl<number | null>(null, { validators: [Validators.required] }),
        orderedQuantity: new FormControl(1, { nonNullable: true, validators: [Validators.required, Validators.min(0.0001)] }),
        unitCost: new FormControl(0, { nonNullable: true, validators: [Validators.required, Validators.min(0)] }),
      }) as PurchaseOrderLineFormGroup,
    );
  }

  removeLine(index: number): void {
    if (this.lines.length > 1) {
      this.lines.removeAt(index);
    }
  }

  submitCreate(): void {
    if (this.createForm.invalid || this.creatingOrder()) {
      this.createForm.markAllAsTouched();
      return;
    }

    const value = this.createForm.getRawValue();
    this.creatingOrder.set(true);
    this.purchasingService
      .createPurchaseOrder({
        supplierId: value.supplierId!,
        notes: value.notes,
        lines: value.lines.map((l) => ({
          itemId: l.itemId!,
          unitOfMeasureId: l.unitOfMeasureId!,
          orderedQuantity: l.orderedQuantity,
          unitCost: l.unitCost,
        })),
      })
      .pipe(finalize(() => this.creatingOrder.set(false)))
      .subscribe({
        next: (created) => {
          this.notification.success(this.i18n.t('purchaseOrders.toasts.created', { orderNumber: created.orderNumber }));
          this.createForm.reset({ supplierId: null, notes: '', lines: [] });
          this.lines.clear();
          this.addLine();
          this.loadOrders();
        },
      });
  }

  selectOrder(summary: PurchaseOrderSummaryDto): void {
    this.loadingDetail.set(true);
    this.receivingLine.set(null);
    this.purchasingService
      .getPurchaseOrder(summary.id)
      .pipe(finalize(() => this.loadingDetail.set(false)))
      .subscribe({ next: (detail) => this.selectedOrder.set(detail) });
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
    const order = this.selectedOrder();
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
          this.selectedOrder.set(updated);
          this.loadOrders();
        },
      });
  }

  submitCancel(): void {
    const order = this.selectedOrder();
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
          this.selectedOrder.set(updated);
          this.receivingLine.set(null);
          this.loadOrders();
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
    const order = this.selectedOrder();
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
          this.selectedOrder.set(updated);
          this.receivingLine.set(null);
          this.loadOrders();
        },
      });
  }
}
