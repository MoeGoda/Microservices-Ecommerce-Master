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
import { ItemSummaryDto, LocationDto, StockLevelDto } from '../../../shared/models/warehouse.models';
import { WarehouseService } from '../warehouse.service';

// M — the positive half of AdjustStockCommand, lifted out of
// item-detail's Stock tab into its own dialog. Calls the exact same
// WarehouseService.adjustStock() item-detail already used, always with
// a positive quantityChange — the negative half lives in
// IssueStockDialogComponent, same command, opposite sign.
@Component({
  selector: 'app-adjust-stock-dialog',
  imports: [ReactiveFormsModule, MatButtonModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatProgressSpinnerModule, SearchableSelectComponent, TranslatePipe],
  templateUrl: './adjust-stock-dialog.component.html',
  styleUrl: './adjust-stock-dialog.component.scss',
})
export class AdjustStockDialogComponent implements OnInit {
  readonly items = signal<ItemSummaryDto[]>([]);
  readonly locations = signal<LocationDto[]>([]);
  readonly submitting = signal(false);

  readonly itemLabel = (item: ItemSummaryDto): string => `${item.sku} — ${item.name}`;
  readonly itemValue = (item: ItemSummaryDto): number => item.id;
  readonly locationLabel = (location: LocationDto): string => `${location.code} — ${location.name}`;
  readonly locationValue = (location: LocationDto): number => location.id;

  readonly form = new FormGroup({
    itemId: new FormControl<number | null>(null, { validators: [Validators.required] }),
    locationId: new FormControl<number | null>(null, { validators: [Validators.required] }),
    quantity: new FormControl(1, { nonNullable: true, validators: [Validators.required, Validators.min(0.0001)] }),
    reference: new FormControl('', { nonNullable: true }),
  });

  constructor(
    private readonly dialogRef: MatDialogRef<AdjustStockDialogComponent, StockLevelDto>,
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
    this.submitting.set(true);
    this.warehouseService
      .adjustStock({
        itemId: value.itemId!,
        locationId: value.locationId!,
        quantityChange: value.quantity,
        reference: value.reference,
      })
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: (updated) => {
          this.notification.success(this.i18n.t('adjustments.toasts.created', { quantity: value.quantity }));
          this.dialogRef.close(updated);
        },
      });
  }
}
