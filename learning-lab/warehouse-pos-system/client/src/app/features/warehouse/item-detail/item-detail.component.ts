import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTabsModule } from '@angular/material/tabs';
import { finalize } from 'rxjs';
import { I18nService } from '../../../core/i18n/i18n.service';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { NotificationService } from '../../../core/notifications/notification.service';
import { SearchableSelectComponent } from '../../../shared/components/searchable-select/searchable-select.component';
import {
  BARCODE_TYPES,
  DISCOUNT_TYPES,
  ItemDetailDto,
  ItemPriceHistoryDto,
  LocationDto,
  PromotionDto,
  StockLevelDto,
  TransferStockRequest,
} from '../../../shared/models/warehouse.models';
import { WarehouseService } from '../warehouse.service';

// K — the management half of the former single-page ItemsAdminComponent,
// now its own route (/items/:id). Reads the id from the route instead of
// receiving an ItemSummaryDto from an in-page list selection; barcodes,
// units, variants, pricing, promotions and stock are otherwise unchanged
// from the original detail panel — just regrouped into tabs so the page
// isn't one very long scroll.
@Component({
  selector: 'app-item-detail',
  imports: [
    DatePipe,
    DecimalPipe,
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatCheckboxModule,
    MatChipsModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTabsModule,
    SearchableSelectComponent,
    TranslatePipe,
  ],
  templateUrl: './item-detail.component.html',
  styleUrl: './item-detail.component.scss',
})
export class ItemDetailComponent implements OnInit {
  readonly barcodeTypes = BARCODE_TYPES;
  readonly discountTypes = DISCOUNT_TYPES;

  readonly locationLabel = (location: LocationDto): string => `${location.code} — ${location.name}`;
  readonly locationValue = (location: LocationDto): number => location.id;

  readonly locations = signal<LocationDto[]>([]);
  readonly selectedItem = signal<ItemDetailDto | null>(null);
  readonly stockLevels = signal<StockLevelDto[]>([]);
  readonly priceHistory = signal<ItemPriceHistoryDto[]>([]);
  readonly promotions = signal<PromotionDto[]>([]);

  readonly loadingDetail = signal(false);
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

  private itemId = 0;

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
    private readonly i18n: I18nService,
    private readonly route: ActivatedRoute,
  ) {}

  ngOnInit(): void {
    this.itemId = Number(this.route.snapshot.paramMap.get('id'));
    this.warehouseService.getLocations().subscribe({ next: (locations) => this.locations.set(locations) });
    this.loadItem();
  }

  private loadItem(): void {
    this.loadingDetail.set(true);
    this.warehouseService
      .getItem(this.itemId)
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
          // already discounted when a promotion is active, and editing
          // that would silently overwrite the item's real list price with
          // whatever the discount happened to compute to. originalUnitPrice
          // falls back to null only when there's NO active promotion, in
          // which case unitPrice IS the real price.
          this.priceForm.reset({ newPrice: detail.originalUnitPrice ?? detail.unitPrice });
          this.promotionForm.reset({ discountType: 'PercentageOff', discountValue: 0, startsAt: '', endsAt: '' });
          this.loadStockLevels();
          this.loadPriceHistory();
          this.loadPromotions();
        },
      });
  }

  private loadPromotions(): void {
    this.warehouseService.getPromotions(this.itemId).subscribe({
      next: (promotions) => this.promotions.set(promotions),
    });
  }

  private loadStockLevels(): void {
    this.warehouseService.getStockLevels(this.itemId).subscribe({
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

  private loadPriceHistory(): void {
    this.warehouseService.getPriceHistory(this.itemId).subscribe({
      next: (history) => this.priceHistory.set(history),
    });
  }

  submitAddBarcode(): void {
    if (this.addBarcodeForm.invalid || this.addingBarcode()) {
      this.addBarcodeForm.markAllAsTouched();
      return;
    }

    const value = this.addBarcodeForm.getRawValue();
    this.addingBarcode.set(true);
    this.warehouseService
      .addBarcode(this.itemId, value)
      .pipe(finalize(() => this.addingBarcode.set(false)))
      .subscribe({
        next: () => {
          this.notification.success(this.i18n.t('items.toasts.barcodeAdded'));
          this.addBarcodeForm.reset({ barcode: '', barcodeType: 'EAN13', isPrimary: false });
          this.loadItem();
        },
      });
  }

  submitReceive(): void {
    if (this.receiveForm.invalid || this.receivingStock()) {
      this.receiveForm.markAllAsTouched();
      return;
    }

    const value = this.receiveForm.getRawValue();
    this.receivingStock.set(true);
    this.warehouseService
      .receiveStock({
        itemId: this.itemId,
        locationId: value.locationId!,
        quantity: value.quantity,
        unitOfMeasureId: value.unitOfMeasureId!,
        reference: value.reference,
      })
      .pipe(finalize(() => this.receivingStock.set(false)))
      .subscribe({
        next: (level) => {
          this.notification.success(
            this.i18n.t('items.toasts.stockReceived', { quantity: level.quantityOnHand, unit: level.unitOfMeasureCode, location: level.locationName }),
          );
          this.loadStockLevels();
        },
      });
  }

  submitAdjust(): void {
    if (this.adjustForm.invalid || this.adjustingStock()) {
      this.adjustForm.markAllAsTouched();
      return;
    }

    const value = this.adjustForm.getRawValue();
    this.adjustingStock.set(true);
    this.warehouseService
      .adjustStock({
        itemId: this.itemId,
        locationId: value.locationId!,
        quantityChange: value.quantityChange,
        reference: value.reference,
      })
      .pipe(finalize(() => this.adjustingStock.set(false)))
      .subscribe({
        next: (level) => {
          this.notification.success(
            this.i18n.t('items.toasts.stockAdjusted', { quantity: level.quantityOnHand, unit: level.unitOfMeasureCode, location: level.locationName }),
          );
          this.loadStockLevels();
        },
      });
  }

  submitTransfer(): void {
    if (this.transferForm.invalid || this.transferringStock()) {
      this.transferForm.markAllAsTouched();
      return;
    }

    const value = this.transferForm.getRawValue();
    if (value.fromLocationId === value.toLocationId) {
      this.notification.error(this.i18n.t('items.toasts.transferLocationsMustDiffer'));
      return;
    }

    const request: TransferStockRequest = {
      itemId: this.itemId,
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
            this.i18n.t('items.toasts.transferred', {
              quantity: value.quantity,
              unit: result.from.unitOfMeasureCode,
              from: result.from.locationName,
              to: result.to.locationName,
            }),
          );
          this.loadStockLevels();
        },
      });
  }

  submitUpdatePrice(): void {
    if (this.priceForm.invalid || this.updatingPrice()) {
      this.priceForm.markAllAsTouched();
      return;
    }

    const { newPrice } = this.priceForm.getRawValue();
    this.updatingPrice.set(true);
    this.warehouseService
      .updatePrice(this.itemId, { newPrice })
      .pipe(finalize(() => this.updatingPrice.set(false)))
      .subscribe({
        next: (updated) => {
          this.notification.success(this.i18n.t('items.toasts.priceUpdated', { price: updated.originalUnitPrice ?? updated.unitPrice }));
          this.loadItem();
        },
      });
  }

  submitCreatePromotion(): void {
    if (this.promotionForm.invalid || this.creatingPromotion()) {
      this.promotionForm.markAllAsTouched();
      return;
    }

    const value = this.promotionForm.getRawValue();
    this.creatingPromotion.set(true);
    this.warehouseService
      .createPromotion(this.itemId, {
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
          this.notification.success(this.i18n.t('items.toasts.promotionCreated'));
          this.promotionForm.reset({ discountType: 'PercentageOff', discountValue: 0, startsAt: '', endsAt: '' });
          // Re-fetching the item picks up the discounted price immediately
          // if the new promotion is already active (StartsAtUtc <= now).
          this.loadItem();
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
    if (this.cancellingPromotionId()) {
      return;
    }

    this.cancellingPromotionId.set(promotion.id);
    this.warehouseService
      .cancelPromotion(this.itemId, promotion.id)
      .pipe(finalize(() => this.cancellingPromotionId.set(null)))
      .subscribe({
        next: () => {
          this.notification.success(this.i18n.t('items.toasts.promotionCancelled'));
          // Re-fetching the item picks up the base price immediately if
          // the cancelled promotion was the one currently discounting it.
          this.loadItem();
        },
      });
  }
}
