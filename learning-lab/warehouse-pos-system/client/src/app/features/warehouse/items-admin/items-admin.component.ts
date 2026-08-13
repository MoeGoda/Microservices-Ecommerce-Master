import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { finalize, forkJoin } from 'rxjs';
import { NotificationService } from '../../../core/notifications/notification.service';
import {
  BARCODE_TYPES,
  CategoryDto,
  DISCOUNT_TYPES,
  ItemDetailDto,
  ItemPriceHistoryDto,
  ItemSummaryDto,
  LocationDto,
  PromotionDto,
  StockLevelDto,
  TransferStockRequest,
  UnitOfMeasureDto,
} from '../../../shared/models/warehouse.models';
import { WarehouseService } from '../warehouse.service';

// The B3 Admin Panel screen: create items, browse the catalog, and manage
// one selected item's barcodes/units/stock. Deliberately one component
// rather than several routed sub-pages — there's no lazy-loading/nested-
// routing precedent anywhere else in this app yet (see A4), and a single
// screen with a selection panel is simple enough not to need one.
@Component({
  selector: 'app-items-admin',
  imports: [
    DatePipe,
    DecimalPipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatCheckboxModule,
    MatChipsModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
  ],
  templateUrl: './items-admin.component.html',
  styleUrl: './items-admin.component.scss',
})
export class ItemsAdminComponent implements OnInit {
  readonly barcodeTypes = BARCODE_TYPES;
  readonly discountTypes = DISCOUNT_TYPES;

  readonly categories = signal<CategoryDto[]>([]);
  readonly locations = signal<LocationDto[]>([]);
  readonly units = signal<UnitOfMeasureDto[]>([]);
  readonly items = signal<ItemSummaryDto[]>([]);
  readonly selectedItem = signal<ItemDetailDto | null>(null);
  readonly stockLevels = signal<StockLevelDto[]>([]);
  readonly priceHistory = signal<ItemPriceHistoryDto[]>([]);
  readonly promotions = signal<PromotionDto[]>([]);

  readonly loadingItems = signal(false);
  readonly loadingDetail = signal(false);
  readonly creatingItem = signal(false);
  readonly addingBarcode = signal(false);
  readonly receivingStock = signal(false);
  readonly adjustingStock = signal(false);
  readonly transferringStock = signal(false);
  readonly updatingPrice = signal(false);
  readonly creatingPromotion = signal(false);
  // The one promotion currently being cancelled, if any — disables just
  // that row's button rather than every button in the list while the
  // request is in flight.
  readonly cancellingPromotionId = signal<number | null>(null);

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

  readonly addBarcodeForm = new FormGroup({
    barcode: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    barcodeType: new FormControl<string>('EAN13', { nonNullable: true, validators: [Validators.required] }),
    isPrimary: new FormControl(false, { nonNullable: true }),
  });

  readonly receiveForm = new FormGroup({
    locationId: new FormControl<number | null>(null, { validators: [Validators.required] }),
    unitOfMeasureId: new FormControl<number | null>(null, { validators: [Validators.required] }),
    quantity: new FormControl(1, { nonNullable: true, validators: [Validators.required, Validators.min(0.0001)] }),
    reference: new FormControl('', { nonNullable: true }),
  });

  readonly adjustForm = new FormGroup({
    locationId: new FormControl<number | null>(null, { validators: [Validators.required] }),
    quantityChange: new FormControl(0, { nonNullable: true, validators: [Validators.required] }),
    reference: new FormControl('', { nonNullable: true }),
  });

  readonly transferForm = new FormGroup({
    fromLocationId: new FormControl<number | null>(null, { validators: [Validators.required] }),
    toLocationId: new FormControl<number | null>(null, { validators: [Validators.required] }),
    quantity: new FormControl(1, { nonNullable: true, validators: [Validators.required, Validators.min(1)] }),
    reference: new FormControl('', { nonNullable: true }),
  });

  readonly priceForm = new FormGroup({
    newPrice: new FormControl(0, { nonNullable: true, validators: [Validators.required, Validators.min(0)] }),
  });

  // startsAt/endsAt are native datetime-local strings ("YYYY-MM-DDTHH:mm",
  // in the BROWSER'S OWN timezone, not UTC) — converted to a UTC ISO
  // string in submitCreatePromotion() before this ever reaches
  // CreatePromotionRequest, which — like every other timestamp in this
  // app — is UTC end to end.
  readonly promotionForm = new FormGroup({
    discountType: new FormControl<string>('PercentageOff', { nonNullable: true, validators: [Validators.required] }),
    discountValue: new FormControl(0, { nonNullable: true, validators: [Validators.required, Validators.min(0.01)] }),
    startsAt: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    endsAt: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  constructor(
    private readonly warehouseService: WarehouseService,
    private readonly notification: NotificationService,
  ) {}

  ngOnInit(): void {
    // All three lookups + the item list load together — none depends on
    // another, so there's no reason to chain them one after another.
    forkJoin({
      categories: this.warehouseService.getCategories(),
      locations: this.warehouseService.getLocations(),
      units: this.warehouseService.getUnitsOfMeasure(),
    }).subscribe(({ categories, locations, units }) => {
      this.categories.set(categories);
      this.locations.set(locations);
      this.units.set(units);
    });

    this.loadItems();
  }

  loadItems(): void {
    this.loadingItems.set(true);
    this.warehouseService
      .getItems()
      .pipe(finalize(() => this.loadingItems.set(false)))
      .subscribe({ next: (items) => this.items.set(items) });
  }

  selectItem(item: ItemSummaryDto): void {
    this.loadingDetail.set(true);
    this.warehouseService
      .getItem(item.id)
      .pipe(finalize(() => this.loadingDetail.set(false)))
      .subscribe({
        next: (detail) => {
          this.selectedItem.set(detail);
          // Receiving defaults to the item's own base unit — the common
          // case — but any alternate unit the item supports is still
          // selectable (see the template).
          this.receiveForm.reset({ locationId: null, unitOfMeasureId: detail.baseUnitOfMeasureId, quantity: 1, reference: '' });
          this.addBarcodeForm.reset({ barcode: '', barcodeType: 'EAN13', isPrimary: false });
          // originalUnitPrice, not unitPrice — unitPrice on this DTO is
          // already discounted when a promotion is active (C5), and
          // editing that would silently overwrite the item's real list
          // price with whatever the discount happened to compute to.
          // originalUnitPrice falls back to null only when there's NO
          // active promotion, in which case unitPrice IS the real price.
          this.priceForm.reset({ newPrice: detail.originalUnitPrice ?? detail.unitPrice });
          this.promotionForm.reset({ discountType: 'PercentageOff', discountValue: 0, startsAt: '', endsAt: '' });
          this.loadStockLevels(item.id);
          this.loadPriceHistory(item.id);
          this.loadPromotions(item.id);
        },
      });
  }

  private loadPromotions(itemId: number): void {
    this.warehouseService.getPromotions(itemId).subscribe({
      next: (promotions) => this.promotions.set(promotions),
    });
  }

  private loadStockLevels(itemId: number): void {
    this.warehouseService.getStockLevels(itemId).subscribe({
      next: (levels) => {
        this.stockLevels.set(levels);
        // Adjusting requires a StockLevel that already exists — default to
        // the first one on hand rather than leaving the picker empty.
        this.adjustForm.reset({ locationId: levels[0]?.locationId ?? null, quantityChange: 0, reference: '' });
        // Transferring FROM also requires an existing balance; TO is any
        // location at all (transferStock creates the destination row if
        // it doesn't exist yet) — defaulting it to the second stocked
        // location, if there is one, just avoids the obviously-wrong
        // same-location default the picker would otherwise start on.
        this.transferForm.reset({
          fromLocationId: levels[0]?.locationId ?? null,
          toLocationId: levels[1]?.locationId ?? this.locations().find((l) => l.id !== levels[0]?.locationId)?.id ?? null,
          quantity: 1,
          reference: '',
        });
      },
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
          this.notification.success(`Item "${created.name}" created.`);
          this.createForm.reset({ sku: '', name: '', description: '', unitPrice: 0, categoryId: null, baseUnitOfMeasureId: null, parentItemId: null, barcode: '', barcodeType: 'EAN13' });
          this.loadItems();
        },
      });
  }

  submitAddBarcode(): void {
    const item = this.selectedItem();
    if (!item || this.addBarcodeForm.invalid || this.addingBarcode()) {
      this.addBarcodeForm.markAllAsTouched();
      return;
    }

    const value = this.addBarcodeForm.getRawValue();
    this.addingBarcode.set(true);
    this.warehouseService
      .addBarcode(item.id, value)
      .pipe(finalize(() => this.addingBarcode.set(false)))
      .subscribe({
        next: () => {
          this.notification.success('Barcode added.');
          this.addBarcodeForm.reset({ barcode: '', barcodeType: 'EAN13', isPrimary: false });
          this.selectItem(item);
        },
      });
  }

  submitReceive(): void {
    const item = this.selectedItem();
    if (!item || this.receiveForm.invalid || this.receivingStock()) {
      this.receiveForm.markAllAsTouched();
      return;
    }

    const value = this.receiveForm.getRawValue();
    this.receivingStock.set(true);
    this.warehouseService
      .receiveStock({
        itemId: item.id,
        locationId: value.locationId!,
        quantity: value.quantity,
        unitOfMeasureId: value.unitOfMeasureId!,
        reference: value.reference,
      })
      .pipe(finalize(() => this.receivingStock.set(false)))
      .subscribe({
        next: (level) => {
          this.notification.success(`Stock received — now ${level.quantityOnHand} ${level.unitOfMeasureCode} at ${level.locationName}.`);
          this.loadStockLevels(item.id);
        },
      });
  }

  submitAdjust(): void {
    const item = this.selectedItem();
    if (!item || this.adjustForm.invalid || this.adjustingStock()) {
      this.adjustForm.markAllAsTouched();
      return;
    }

    const value = this.adjustForm.getRawValue();
    this.adjustingStock.set(true);
    this.warehouseService
      .adjustStock({
        itemId: item.id,
        locationId: value.locationId!,
        quantityChange: value.quantityChange,
        reference: value.reference,
      })
      .pipe(finalize(() => this.adjustingStock.set(false)))
      .subscribe({
        next: (level) => {
          this.notification.success(`Stock adjusted — now ${level.quantityOnHand} ${level.unitOfMeasureCode} at ${level.locationName}.`);
          this.loadStockLevels(item.id);
        },
      });
  }

  submitTransfer(): void {
    const item = this.selectedItem();
    if (!item || this.transferForm.invalid || this.transferringStock()) {
      this.transferForm.markAllAsTouched();
      return;
    }

    const value = this.transferForm.getRawValue();
    if (value.fromLocationId === value.toLocationId) {
      this.notification.error('From and to locations must be different.');
      return;
    }

    const request: TransferStockRequest = {
      itemId: item.id,
      fromLocationId: value.fromLocationId!,
      toLocationId: value.toLocationId!,
      quantity: value.quantity,
      reference: value.reference,
    };

    this.transferringStock.set(true);
    this.warehouseService
      .transferStock(request)
      .pipe(finalize(() => this.transferringStock.set(false)))
      .subscribe({
        next: (result) => {
          this.notification.success(
            `Transferred ${value.quantity} ${result.from.unitOfMeasureCode} from ${result.from.locationName} to ${result.to.locationName}.`,
          );
          this.loadStockLevels(item.id);
        },
      });
  }

  private loadPriceHistory(itemId: number): void {
    this.warehouseService.getPriceHistory(itemId).subscribe({
      next: (history) => this.priceHistory.set(history),
    });
  }

  submitUpdatePrice(): void {
    const item = this.selectedItem();
    if (!item || this.priceForm.invalid || this.updatingPrice()) {
      this.priceForm.markAllAsTouched();
      return;
    }

    const { newPrice } = this.priceForm.getRawValue();
    this.updatingPrice.set(true);
    this.warehouseService
      .updatePrice(item.id, { newPrice })
      .pipe(finalize(() => this.updatingPrice.set(false)))
      .subscribe({
        next: (updated) => {
          this.notification.success(`Price updated to ${updated.originalUnitPrice ?? updated.unitPrice}.`);
          this.selectItem(item);
          this.loadItems();
        },
      });
  }

  submitCreatePromotion(): void {
    const item = this.selectedItem();
    if (!item || this.promotionForm.invalid || this.creatingPromotion()) {
      this.promotionForm.markAllAsTouched();
      return;
    }

    const value = this.promotionForm.getRawValue();
    this.creatingPromotion.set(true);
    this.warehouseService
      .createPromotion(item.id, {
        discountType: value.discountType,
        discountValue: value.discountValue,
        // datetime-local gives local-time strings with no timezone
        // marker — new Date(...) interprets that as the BROWSER's local
        // time, and toISOString() converts it to UTC, matching what
        // CreatePromotionCommand expects (StartsAtUtc/EndsAtUtc).
        startsAtUtc: new Date(value.startsAt).toISOString(),
        endsAtUtc: new Date(value.endsAt).toISOString(),
      })
      .pipe(finalize(() => this.creatingPromotion.set(false)))
      .subscribe({
        next: () => {
          this.notification.success('Promotion created.');
          this.promotionForm.reset({ discountType: 'PercentageOff', discountValue: 0, startsAt: '', endsAt: '' });
          // Re-fetching the item picks up the discounted price immediately
          // if the new promotion is already active (StartsAtUtc <= now).
          this.selectItem(item);
        },
      });
  }

  // Plain TS, not template date-string comparisons — comparing ISO
  // strings lexicographically happens to work but is the wrong tool;
  // parsing once here keeps the template a pure display layer.
  promotionStatus(promotion: PromotionDto): 'Cancelled' | 'Expired' | 'Upcoming' | 'Active' {
    if (promotion.isCancelled) {
      return 'Cancelled';
    }

    const now = new Date();
    if (new Date(promotion.endsAtUtc) < now) {
      return 'Expired';
    }
    if (new Date(promotion.startsAtUtc) > now) {
      return 'Upcoming';
    }

    return 'Active';
  }

  canCancelPromotion(promotion: PromotionDto): boolean {
    return !promotion.isCancelled && new Date(promotion.endsAtUtc) >= new Date();
  }

  cancelPromotion(promotion: PromotionDto): void {
    const item = this.selectedItem();
    if (!item || this.cancellingPromotionId()) {
      return;
    }

    this.cancellingPromotionId.set(promotion.id);
    this.warehouseService
      .cancelPromotion(item.id, promotion.id)
      .pipe(finalize(() => this.cancellingPromotionId.set(null)))
      .subscribe({
        next: () => {
          this.notification.success('Promotion cancelled.');
          // Re-fetching the item picks up the base price immediately if
          // the cancelled promotion was the one currently discounting it.
          this.selectItem(item);
        },
      });
  }
}
