import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, ElementRef, Injector, OnInit, ViewChild, afterNextRender, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { finalize } from 'rxjs';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { NotificationService } from '../../core/notifications/notification.service';
import { LocationDto } from '../../shared/models/warehouse.models';
import { SaleDto } from '../../shared/models/pos.models';
import { WarehouseService } from '../warehouse/warehouse.service';
import { PosService } from './pos.service';

// The C4 register screen: one component drives all three states a sale
// moves through (no active sale → InProgress → Completed), the same
// single-screen-with-a-selection-panel reasoning items-admin.component.ts
// (B3) used rather than several routed sub-pages.
@Component({
  selector: 'app-pos-register',
  imports: [
    DatePipe,
    DecimalPipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    TranslatePipe,
  ],
  templateUrl: './pos-register.component.html',
  styleUrl: './pos-register.component.scss',
})
export class PosRegisterComponent implements OnInit {
  readonly locations = signal<LocationDto[]>([]);
  readonly sale = signal<SaleDto | null>(null);

  readonly startingSale = signal(false);
  readonly addingLine = signal(false);
  readonly removingLineId = signal<number | null>(null);
  readonly checkingOut = signal(false);
  readonly cancelling = signal(false);
  readonly returningSale = signal(false);

  readonly startForm = new FormGroup({
    locationId: new FormControl<number | null>(null, { validators: [Validators.required] }),
  });

  // Barcode + quantity, the two things a physical scanner (or a cashier
  // typing) supplies per line. A scanner types the barcode's digits
  // followed by an Enter keystroke, indistinguishable to the DOM from a
  // person hitting Enter by hand — (keyup.enter) below submitting this
  // form is what makes the field genuinely "scan-ready" rather than
  // needing a dedicated scan button.
  readonly scanForm = new FormGroup({
    barcode: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    quantity: new FormControl(1, { nonNullable: true, validators: [Validators.required, Validators.min(1)] }),
  });

  @ViewChild('barcodeInput') private readonly barcodeInput?: ElementRef<HTMLInputElement>;

  constructor(
    private readonly pos: PosService,
    private readonly warehouseService: WarehouseService,
    private readonly notification: NotificationService,
    private readonly injector: Injector,
  ) {}

  ngOnInit(): void {
    this.warehouseService.getLocations().subscribe((locations) => {
      this.locations.set(locations);
      this.startForm.patchValue({ locationId: locations[0]?.id ?? null });
    });
  }

  submitStartSale(): void {
    if (this.startForm.invalid || this.startingSale()) {
      this.startForm.markAllAsTouched();
      return;
    }

    const { locationId } = this.startForm.getRawValue();
    this.startingSale.set(true);
    this.pos
      .startSale({ locationId: locationId! })
      .pipe(finalize(() => this.startingSale.set(false)))
      .subscribe({
        next: (sale) => {
          this.sale.set(sale);
          this.focusBarcodeInput();
        },
      });
  }

  submitScan(): void {
    const sale = this.sale();
    if (!sale || this.scanForm.invalid || this.addingLine()) {
      this.scanForm.markAllAsTouched();
      return;
    }

    const { barcode, quantity } = this.scanForm.getRawValue();
    this.addingLine.set(true);
    this.pos
      .addLine(sale.id, { barcode, quantity })
      .pipe(finalize(() => this.addingLine.set(false)))
      .subscribe({
        // An unknown barcode (404) or short stock (409) is already turned
        // into a toast by errorInterceptor — this only needs the success
        // path, same discipline items-admin.component.ts follows.
        next: (updated) => {
          this.sale.set(updated);
          this.scanForm.reset({ barcode: '', quantity: 1 });
          this.focusBarcodeInput();
        },
      });
  }

  removeLine(lineId: number): void {
    const sale = this.sale();
    if (!sale || this.removingLineId() !== null) {
      return;
    }

    this.removingLineId.set(lineId);
    this.pos
      .removeLine(sale.id, lineId)
      .pipe(finalize(() => this.removingLineId.set(null)))
      .subscribe({ next: (updated) => this.sale.set(updated) });
  }

  submitCheckout(): void {
    const sale = this.sale();
    if (!sale || sale.lines.length === 0 || this.checkingOut()) {
      return;
    }

    this.checkingOut.set(true);
    this.pos
      .checkout(sale.id)
      .pipe(finalize(() => this.checkingOut.set(false)))
      .subscribe({
        next: (completed) => {
          this.sale.set(completed);
          this.notification.success(`Sale #${completed.id} completed — total ${completed.total.toFixed(2)}.`);
        },
      });
  }

  cancelSale(): void {
    const sale = this.sale();
    if (!sale || this.cancelling()) {
      return;
    }

    this.cancelling.set(true);
    this.pos
      .cancelSale(sale.id)
      .pipe(finalize(() => this.cancelling.set(false)))
      .subscribe({
        next: () => {
          this.sale.set(null);
          this.notification.success('Sale cancelled.');
        },
      });
  }

  returnSale(): void {
    const sale = this.sale();
    if (!sale || this.returningSale()) {
      return;
    }

    this.returningSale.set(true);
    this.pos
      .returnSale(sale.id)
      .pipe(finalize(() => this.returningSale.set(false)))
      .subscribe({
        next: (returned) => {
          this.sale.set(returned);
          this.notification.success(`Sale #${returned.id} returned.`);
        },
      });
  }

  startNewSale(): void {
    this.sale.set(null);
    this.scanForm.reset({ barcode: '', quantity: 1 });
  }

  private focusBarcodeInput(): void {
    // The input isn't in the DOM yet on the very first call — it only
    // renders once sale() flips to the InProgress result of
    // startSale/addLine, and a plain setTimeout(0) isn't a reliable proxy
    // for "Angular has finished rendering that": on the very first
    // transition (no sale -> InProgress) the ViewChild query hadn't
    // updated yet by the time a bare macrotask fired, though it happened
    // to have by later calls. afterNextRender is the actual guarantee —
    // it runs once Angular has committed the DOM update this signal write
    // triggered, not just "some tick later."
    afterNextRender(() => this.barcodeInput?.nativeElement.focus(), { injector: this.injector });
  }
}
