import { Component, OnInit, signal } from '@angular/core';
import { FormArray, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { finalize, forkJoin } from 'rxjs';
import { I18nService } from '../../../core/i18n/i18n.service';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { NotificationService } from '../../../core/notifications/notification.service';
import { PurchaseOrderDetailDto, SupplierDto } from '../../../shared/models/purchasing.models';
import { ItemDetailDto, ItemSummaryDto } from '../../../shared/models/warehouse.models';
import { WarehouseService } from '../../warehouse/warehouse.service';
import { PurchasingService } from '../purchasing.service';

// Named so addLine()/getRawValue() give `lines` a concrete shape — an
// inline FormGroup literal infers an index-signature type that
// TypeScript's noPropertyAccessFromIndexSignature then forbids dotted
// access on (l.itemId, etc.) in submitCreate().
type PurchaseOrderLineFormGroup = FormGroup<{
  itemId: FormControl<number | null>;
  unitOfMeasureId: FormControl<number | null>;
  orderedQuantity: FormControl<number>;
  unitCost: FormControl<number>;
}>;

// L — the create half of what used to be an inline card above the
// Purchase Orders grid. Keeps the unit-conversion bug fix intact: a
// line's unit picker is still scoped to the selected item's own valid
// units (base unit + ItemUnit alternates), not every UnitOfMeasure in
// the system — ordering a unit the item has no conversion for used to
// create a PO that could never actually be received.
@Component({
  selector: 'app-purchase-order-create-dialog',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    TranslatePipe,
  ],
  templateUrl: './purchase-order-create-dialog.component.html',
  styleUrl: './purchase-order-create-dialog.component.scss',
})
export class PurchaseOrderCreateDialogComponent implements OnInit {
  readonly suppliers = signal<SupplierDto[]>([]);
  readonly items = signal<ItemSummaryDto[]>([]);
  readonly creatingOrder = signal(false);

  private readonly itemDetailCache = new Map<number, ItemDetailDto>();

  readonly createForm = new FormGroup({
    supplierId: new FormControl<number | null>(null, { validators: [Validators.required] }),
    notes: new FormControl('', { nonNullable: true }),
    lines: new FormArray<PurchaseOrderLineFormGroup>([]),
  });

  get lines(): FormArray<PurchaseOrderLineFormGroup> {
    return this.createForm.controls.lines;
  }

  constructor(
    private readonly dialogRef: MatDialogRef<PurchaseOrderCreateDialogComponent, PurchaseOrderDetailDto>,
    private readonly purchasingService: PurchasingService,
    private readonly warehouseService: WarehouseService,
    private readonly notification: NotificationService,
    private readonly i18n: I18nService,
  ) {}

  ngOnInit(): void {
    forkJoin({
      suppliers: this.purchasingService.getSuppliers(1, 100),
      items: this.warehouseService.getItems(1, 100),
    }).subscribe(({ suppliers, items }) => {
      this.suppliers.set(suppliers.items);
      this.items.set(items.items);
    });

    this.addLine();
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

  // Fetches (once, then cached) the units the line's newly-picked item
  // actually supports, and snaps the unit field to that item's base unit
  // if the previously-selected unit isn't one of them.
  onLineItemChange(line: PurchaseOrderLineFormGroup): void {
    const itemId = line.controls.itemId.value;
    if (itemId == null) {
      return;
    }

    const cached = this.itemDetailCache.get(itemId);
    if (cached) {
      this.reconcileLineUnit(line, cached);
      return;
    }

    this.warehouseService.getItem(itemId).subscribe({
      next: (detail) => {
        this.itemDetailCache.set(itemId, detail);
        this.reconcileLineUnit(line, detail);
      },
    });
  }

  private reconcileLineUnit(line: PurchaseOrderLineFormGroup, item: ItemDetailDto): void {
    const validIds = new Set([item.baseUnitOfMeasureId, ...item.units.map((u) => u.unitOfMeasureId)]);
    const current = line.controls.unitOfMeasureId.value;
    if (current == null || !validIds.has(current)) {
      line.controls.unitOfMeasureId.setValue(item.baseUnitOfMeasureId);
    }
  }

  // Base unit first, then any alternates — same order item-detail's own
  // receive form lists an item's units in.
  validUnitsForLine(line: PurchaseOrderLineFormGroup): { id: number; code: string }[] {
    const itemId = line.controls.itemId.value;
    const detail = itemId == null ? undefined : this.itemDetailCache.get(itemId);
    if (!detail) {
      return [];
    }

    return [{ id: detail.baseUnitOfMeasureId, code: detail.baseUnitOfMeasureCode }, ...detail.units.map((u) => ({ id: u.unitOfMeasureId, code: u.unitOfMeasureCode }))];
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
          this.dialogRef.close(created);
        },
      });
  }
}
