import { Component, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { finalize, forkJoin } from 'rxjs';
import { I18nService } from '../../../core/i18n/i18n.service';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { NotificationService } from '../../../core/notifications/notification.service';
import { BARCODE_TYPES, CategoryDto, ItemSummaryDto, UnitOfMeasureDto } from '../../../shared/models/warehouse.models';
import { WarehouseService } from '../warehouse.service';

// K — the create half of the former single-page ItemsAdminComponent, now
// its own route (/items/new). On success this navigates straight to the
// new item's detail page (/items/:id) rather than back to the list — the
// natural next step after creating something is usually to keep working
// on it (add a second barcode, set stock), not to go looking at it in a
// table row.
@Component({
  selector: 'app-item-create',
  imports: [ReactiveFormsModule, MatButtonModule, MatCardModule, MatFormFieldModule, MatInputModule, MatProgressSpinnerModule, MatSelectModule, TranslatePipe],
  templateUrl: './item-create.component.html',
  styleUrl: './item-create.component.scss',
})
export class ItemCreateComponent implements OnInit {
  readonly barcodeTypes = BARCODE_TYPES;

  readonly categories = signal<CategoryDto[]>([]);
  readonly units = signal<UnitOfMeasureDto[]>([]);
  // Same page-size-100 pragmatic limitation ItemsAdminComponent's own
  // parentCandidates always had — see that component's own comment
  // (now here, since this is the only place that still needs the picker).
  readonly parentCandidates = signal<ItemSummaryDto[]>([]);
  readonly creatingItem = signal(false);

  readonly createForm = new FormGroup({
    sku: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    name: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    description: new FormControl('', { nonNullable: true }),
    unitPrice: new FormControl(0, { nonNullable: true, validators: [Validators.required, Validators.min(0)] }),
    categoryId: new FormControl<number | null>(null, { validators: [Validators.required] }),
    baseUnitOfMeasureId: new FormControl<number | null>(null, { validators: [Validators.required] }),
    parentItemId: new FormControl<number | null>(null),
    barcode: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    barcodeType: new FormControl<string>('EAN13', { nonNullable: true, validators: [Validators.required] }),
  });

  constructor(
    private readonly warehouseService: WarehouseService,
    private readonly notification: NotificationService,
    private readonly i18n: I18nService,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    forkJoin({
      categories: this.warehouseService.getCategories(),
      units: this.warehouseService.getUnitsOfMeasure(),
      parentCandidates: this.warehouseService.getItems(1, 100),
    }).subscribe(({ categories, units, parentCandidates }) => {
      this.categories.set(categories);
      this.units.set(units);
      this.parentCandidates.set(parentCandidates.items);
    });
  }

  submitCreate(): void {
    if (this.createForm.invalid || this.creatingItem()) {
      this.createForm.markAllAsTouched();
      return;
    }

    const value = this.createForm.getRawValue();
    this.creatingItem.set(true);
    this.warehouseService
      .createItem({
        sku: value.sku,
        name: value.name,
        description: value.description,
        unitPrice: value.unitPrice,
        categoryId: value.categoryId!,
        baseUnitOfMeasureId: value.baseUnitOfMeasureId!,
        parentItemId: value.parentItemId,
        barcode: value.barcode,
        barcodeType: value.barcodeType,
      })
      .pipe(finalize(() => this.creatingItem.set(false)))
      .subscribe({
        // A 4xx (duplicate Sku/barcode, bad input) is already turned into
        // a toast by errorInterceptor — this only needs the success path.
        next: (created) => {
          this.notification.success(this.i18n.t('items.toasts.created', { name: created.name }));
          this.router.navigate(['/items', created.id]);
        },
      });
  }
}
