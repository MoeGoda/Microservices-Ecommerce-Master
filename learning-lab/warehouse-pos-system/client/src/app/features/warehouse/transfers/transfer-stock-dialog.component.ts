import { Component, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize } from 'rxjs';
import { I18nService } from '../../../core/i18n/i18n.service';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { NotificationService } from '../../../core/notifications/notification.service';
import { SearchableSelectComponent } from '../../../shared/components/searchable-select/searchable-select.component';
import { ItemSummaryDto, LocationDto, TransferStockResultDto } from '../../../shared/models/warehouse.models';
import { WarehouseService } from '../warehouse.service';

// M — TransferStockCommand, lifted out of item-detail's Stock tab into
// its own dialog so the new Transfers screen can also start one. Calls
// the exact same WarehouseService.transferStock() item-detail already
// used — no new backend endpoint.
@Component({
  selector: 'app-transfer-stock-dialog',
  imports: [ReactiveFormsModule, MatButtonModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatProgressSpinnerModule, SearchableSelectComponent, TranslatePipe],
  templateUrl: './transfer-stock-dialog.component.html',
  styleUrl: './transfer-stock-dialog.component.scss',
})
export class TransferStockDialogComponent implements OnInit {
  readonly items = signal<ItemSummaryDto[]>([]);
  readonly locations = signal<LocationDto[]>([]);
  readonly submitting = signal(false);

  readonly itemLabel = (item: ItemSummaryDto): string => `${item.sku} — ${item.name}`;
  readonly itemValue = (item: ItemSummaryDto): number => item.id;
  readonly locationLabel = (location: LocationDto): string => `${location.code} — ${location.name}`;
  readonly locationValue = (location: LocationDto): number => location.id;

  readonly form = new FormGroup({
    itemId: new FormControl<number | null>(null, { validators: [Validators.required] }),
    fromLocationId: new FormControl<number | null>(null, { validators: [Validators.required] }),
    toLocationId: new FormControl<number | null>(null, { validators: [Validators.required] }),
    quantity: new FormControl(1, { nonNullable: true, validators: [Validators.required, Validators.min(0.0001)] }),
    reference: new FormControl('', { nonNullable: true }),
  });

  constructor(
    private readonly dialogRef: MatDialogRef<TransferStockDialogComponent, TransferStockResultDto>,
    private readonly warehouseService: WarehouseService,
    private readonly notification: NotificationService,
    private readonly i18n: I18nService,
  ) {}

  ngOnInit(): void {
    this.warehouseService.getItems(1, 100).subscribe((result) => this.items.set(result.items));
    this.warehouseService.getLocations().subscribe((locations) => this.locations.set(locations));
  }

  submit(): void {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    if (value.fromLocationId === value.toLocationId) {
      this.form.controls.toLocationId.setErrors({ sameLocation: true });
      return;
    }

    this.submitting.set(true);
    this.warehouseService
      .transferStock({
        itemId: value.itemId!,
        fromLocationId: value.fromLocationId!,
        toLocationId: value.toLocationId!,
        quantity: value.quantity,
        reference: value.reference,
      })
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: (result) => {
          this.notification.success(this.i18n.t('transfers.toasts.created', { quantity: value.quantity }));
          this.dialogRef.close(result);
        },
      });
  }
}
