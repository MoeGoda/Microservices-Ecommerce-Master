import { Component, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { finalize } from 'rxjs';
import { I18nService } from '../../../core/i18n/i18n.service';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { NotificationService } from '../../../core/notifications/notification.service';
import { SearchableSelectComponent } from '../../../shared/components/searchable-select/searchable-select.component';
import { ItemSummaryDto, LocationDto, StockLevelDto } from '../../../shared/models/warehouse.models';
import { WarehouseService } from '../warehouse.service';

// M — the free-text ("no Purchase Order") receiving flow, lifted out of
// item-detail's Stock tab into its own dialog so the new Receipts
// screen can also start one. Calls the exact same
// WarehouseService.receiveStock() item-detail already used — no new
// backend endpoint, no change to that existing screen.
@Component({
  selector: 'app-receive-stock-dialog',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    SearchableSelectComponent,
    TranslatePipe,
  ],
  templateUrl: './receive-stock-dialog.component.html',
  styleUrl: './receive-stock-dialog.component.scss',
})
export class ReceiveStockDialogComponent implements OnInit {
  readonly items = signal<ItemSummaryDto[]>([]);
  readonly locations = signal<LocationDto[]>([]);
  readonly validUnits = signal<{ id: number; code: string }[]>([]);
  readonly submitting = signal(false);

  readonly itemLabel = (item: ItemSummaryDto): string => `${item.sku} — ${item.name}`;
  readonly itemValue = (item: ItemSummaryDto): number => item.id;
  readonly locationLabel = (location: LocationDto): string => `${location.code} — ${location.name}`;
  readonly locationValue = (location: LocationDto): number => location.id;

  readonly form = new FormGroup({
    itemId: new FormControl<number | null>(null, { validators: [Validators.required] }),
    locationId: new FormControl<number | null>(null, { validators: [Validators.required] }),
    unitOfMeasureId: new FormControl<number | null>(null, { validators: [Validators.required] }),
    quantity: new FormControl(1, { nonNullable: true, validators: [Validators.required, Validators.min(0.0001)] }),
    reference: new FormControl('', { nonNullable: true }),
  });

  constructor(
    private readonly dialogRef: MatDialogRef<ReceiveStockDialogComponent, StockLevelDto>,
    private readonly warehouseService: WarehouseService,
    private readonly notification: NotificationService,
    private readonly i18n: I18nService,
  ) {}

  ngOnInit(): void {
    this.warehouseService.getItems(1, 100).subscribe((result) => this.items.set(result.items));
    this.warehouseService.getLocations().subscribe((locations) => this.locations.set(locations));
  }

  onItemSelected(item: ItemSummaryDto | null): void {
    this.validUnits.set([]);
    this.form.controls.unitOfMeasureId.setValue(null);
    if (!item) {
      return;
    }

    this.warehouseService.getItem(item.id).subscribe((detail) => {
      this.validUnits.set([
        { id: detail.baseUnitOfMeasureId, code: detail.baseUnitOfMeasureCode },
        ...detail.units.map((u) => ({ id: u.unitOfMeasureId, code: u.unitOfMeasureCode })),
      ]);
      this.form.controls.unitOfMeasureId.setValue(detail.baseUnitOfMeasureId);
    });
  }

  submit(): void {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.submitting.set(true);
    this.warehouseService
      .receiveStock({
        itemId: value.itemId!,
        locationId: value.locationId!,
        unitOfMeasureId: value.unitOfMeasureId!,
        quantity: value.quantity,
        reference: value.reference,
      })
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: (updated) => {
          this.notification.success(this.i18n.t('receipts.toasts.created', { quantity: value.quantity }));
          this.dialogRef.close(updated);
        },
      });
  }
}
