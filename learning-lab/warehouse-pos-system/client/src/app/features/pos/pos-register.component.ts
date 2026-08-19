import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, ElementRef, Injector, OnInit, ViewChild, afterNextRender, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatAutocompleteModule, MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { debounceTime, distinctUntilChanged, finalize } from 'rxjs';
import { I18nService } from '../../core/i18n/i18n.service';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { NotificationService } from '../../core/notifications/notification.service';
import { LocationDto } from '../../shared/models/warehouse.models';
import { CashDrawerSessionDto, CustomerDto, SaleDto, SaleLineDto } from '../../shared/models/pos.models';
import { WarehouseService } from '../warehouse/warehouse.service';
import { CustomersService } from './customers.service';
import { PosService } from './pos.service';
import { CashMovementDialogComponent } from './dialogs/cash-movement-dialog.component';
import { CloseCashDrawerDialogComponent } from './dialogs/close-cash-drawer-dialog.component';
import { CustomerEditDialogComponent } from './dialogs/customer-edit-dialog.component';
import { HeldSalesDialogComponent } from './dialogs/held-sales-dialog.component';
import { OpenCashDrawerDialogComponent } from './dialogs/open-cash-drawer-dialog.component';
import { XReportDialogComponent } from './dialogs/x-report-dialog.component';

// The C4 register screen, extended for T8: one component still drives
// all three states a sale moves through (no active sale → InProgress →
// Completed), the same single-screen-with-a-selection-panel reasoning
// items-admin.component.ts (B3) used rather than several routed
// sub-pages — this step adds a customer panel, per-line/receipt
// discounts, a tax-exempt toggle, a held-sales picker, and a cash-drawer
// action bar on top of that same structure, restyled to this app's own
// Material language rather than the ERPLY/XD-POS references' flat-color
// button-grid skin.
@Component({
  selector: 'app-pos-register',
  imports: [
    DatePipe,
    DecimalPipe,
    ReactiveFormsModule,
    MatAutocompleteModule,
    MatButtonModule,
    MatCardModule,
    MatCheckboxModule,
    MatDialogModule,
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

  // Customer search-and-attach — a plain debounced server search rather
  // than SearchableSelectComponent, since that component filters a
  // static already-loaded options array client-side; Customers has no
  // "load every customer up front" list to filter, only a paged search.
  readonly customerSearchControl = new FormControl('', { nonNullable: true });
  readonly customerSearchResults = signal<CustomerDto[]>([]);
  readonly searchingCustomers = signal(false);
  readonly settingCustomer = signal(false);

  readonly settingLineDiscountId = signal<number | null>(null);
  readonly settingReceiptDiscount = signal(false);
  readonly settingTaxExempt = signal(false);

  // Cash-drawer state lives only in this component, for this browser
  // session — there is no "get the currently open session for this
  // location" query on the backend (only GetOpenSession at the
  // repository layer, used server-side to reject a second Open). A page
  // reload legitimately starts from "no known open drawer," the same as
  // a real register: every shift begins with an explicit Open action.
  readonly cashDrawerSession = signal<CashDrawerSessionDto | null>(null);

  @ViewChild('barcodeInput') private readonly barcodeInput?: ElementRef<HTMLInputElement>;

  constructor(
    private readonly pos: PosService,
    private readonly customersService: CustomersService,
    private readonly warehouseService: WarehouseService,
    private readonly notification: NotificationService,
    private readonly i18n: I18nService,
    private readonly injector: Injector,
    private readonly dialog: MatDialog,
  ) {}

  ngOnInit(): void {
    this.warehouseService.getLocations().subscribe((locations) => {
      this.locations.set(locations);
      this.startForm.patchValue({ locationId: locations[0]?.id ?? null });
    });

    this.customerSearchControl.valueChanges.pipe(debounceTime(300), distinctUntilChanged()).subscribe((term) => {
      const trimmed = term.trim();
      if (!trimmed) {
        this.customerSearchResults.set([]);
        return;
      }

      this.searchingCustomers.set(true);
      this.customersService
        .search(trimmed)
        .pipe(finalize(() => this.searchingCustomers.set(false)))
        .subscribe({ next: (page) => this.customerSearchResults.set(page.items) });
    });
  }

  currentLocationId(): number {
    return this.sale()?.locationId ?? this.startForm.getRawValue().locationId ?? this.locations()[0]?.id ?? 0;
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

  openHeldSales(): void {
    this.dialog
      .open(HeldSalesDialogComponent, { data: { locationId: this.currentLocationId() } })
      .afterClosed()
      .subscribe((picked: SaleDto | undefined) => {
        if (picked) {
          this.sale.set(picked);
          this.focusBarcodeInput();
        }
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

  onLineDiscountChange(line: SaleLineDto, rawValue: string): void {
    const sale = this.sale();
    if (!sale || this.settingLineDiscountId() !== null) {
      return;
    }

    const trimmed = rawValue.trim();
    const percent = trimmed === '' ? null : Number(trimmed);
    if (percent !== null && (Number.isNaN(percent) || percent < 0 || percent > 100)) {
      return;
    }

    this.settingLineDiscountId.set(line.id);
    this.pos
      .setLineDiscount(sale.id, line.id, percent)
      .pipe(finalize(() => this.settingLineDiscountId.set(null)))
      .subscribe({ next: (updated) => this.sale.set(updated) });
  }

  selectCustomer(customer: CustomerDto): void {
    this.applyCustomer(customer.id);
    this.customerSearchControl.setValue('', { emitEvent: false });
    this.customerSearchResults.set([]);
  }

  onCustomerOptionSelected(event: MatAutocompleteSelectedEvent): void {
    this.selectCustomer(event.option.value as CustomerDto);
  }

  detachCustomer(): void {
    this.applyCustomer(null);
  }

  openNewCustomerDialog(): void {
    this.dialog
      .open(CustomerEditDialogComponent)
      .afterClosed()
      .subscribe((created: CustomerDto | undefined) => {
        if (created) {
          this.selectCustomer(created);
        }
      });
  }

  openEditCurrentCustomerDialog(): void {
    const sale = this.sale();
    if (!sale?.customerId) {
      return;
    }

    this.customersService.getById(sale.customerId).subscribe((customer) => {
      this.dialog
        .open(CustomerEditDialogComponent, { data: { customer } })
        .afterClosed()
        .subscribe((updated: CustomerDto | undefined) => {
          if (updated) {
            // Only the customer's own name/loyalty/balance may have
            // changed, not anything on the sale itself — re-fetching the
            // sale (rather than patching customerName by hand) keeps this
            // in lockstep with whatever SaleDto.FromEntity actually
            // returns.
            this.pos.getSale(sale.id).subscribe((refreshed) => this.sale.set(refreshed));
          }
        });
    });
  }

  private applyCustomer(customerId: number | null): void {
    const sale = this.sale();
    if (!sale || this.settingCustomer()) {
      return;
    }

    this.settingCustomer.set(true);
    this.pos
      .setCustomer(sale.id, customerId)
      .pipe(finalize(() => this.settingCustomer.set(false)))
      .subscribe({ next: (updated) => this.sale.set(updated) });
  }

  onReceiptDiscountChange(rawValue: string): void {
    const sale = this.sale();
    if (!sale || this.settingReceiptDiscount()) {
      return;
    }

    const trimmed = rawValue.trim();
    const percent = trimmed === '' ? null : Number(trimmed);
    if (percent !== null && (Number.isNaN(percent) || percent < 0 || percent > 100)) {
      return;
    }

    this.settingReceiptDiscount.set(true);
    this.pos
      .setReceiptDiscount(sale.id, percent)
      .pipe(finalize(() => this.settingReceiptDiscount.set(false)))
      .subscribe({ next: (updated) => this.sale.set(updated) });
  }

  onTaxExemptToggle(checked: boolean): void {
    const sale = this.sale();
    if (!sale || this.settingTaxExempt()) {
      return;
    }

    this.settingTaxExempt.set(true);
    this.pos
      .setTaxExempt(sale.id, checked)
      .pipe(finalize(() => this.settingTaxExempt.set(false)))
      .subscribe({ next: (updated) => this.sale.set(updated) });
  }

  lineSubtotal(sale: SaleDto): number {
    return sale.lines.reduce((sum, line) => sum + line.lineTotal, 0);
  }

  receiptDiscountAmount(sale: SaleDto): number {
    return this.lineSubtotal(sale) - sale.netTotal;
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
          this.notification.success(this.i18n.t('pos.toasts.completed', { id: completed.id, total: completed.total.toFixed(2) }));
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
          this.notification.success(this.i18n.t('pos.toasts.cancelled'));
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
          this.notification.success(this.i18n.t('pos.toasts.returned', { id: returned.id }));
        },
      });
  }

  startNewSale(): void {
    this.sale.set(null);
    this.scanForm.reset({ barcode: '', quantity: 1 });
    this.customerSearchControl.setValue('', { emitEvent: false });
    this.customerSearchResults.set([]);
  }

  // "Hold" isn't a distinct backend action — StartSaleCommand never
  // blocked more than one concurrent InProgress sale (T2's own held-sales
  // query relies on exactly that), so leaving a sale InProgress and
  // clearing the local view IS holding it. openHeldSales()/the held-sales
  // dialog is the other half that makes it resumable.
  holdSale(): void {
    this.startNewSale();
  }

  openCashDrawer(): void {
    this.dialog
      .open(OpenCashDrawerDialogComponent, { data: { locationId: this.currentLocationId() } })
      .afterClosed()
      .subscribe((session: CashDrawerSessionDto | undefined) => {
        if (session) {
          this.cashDrawerSession.set(session);
          this.notification.success(this.i18n.t('pos.cashDrawer.toasts.opened'));
        }
      });
  }

  recordCashMovement(type: 'CashIn' | 'CashOut'): void {
    this.dialog.open(CashMovementDialogComponent, { data: { locationId: this.currentLocationId(), type } });
  }

  openXReport(): void {
    const session = this.cashDrawerSession();
    if (!session) {
      return;
    }

    this.dialog.open(XReportDialogComponent, { data: { sessionId: session.id } });
  }

  closeCashDrawer(): void {
    const session = this.cashDrawerSession();
    if (!session) {
      return;
    }

    this.dialog
      .open(CloseCashDrawerDialogComponent, { data: { sessionId: session.id } })
      .afterClosed()
      .subscribe((closed: CashDrawerSessionDto | undefined) => {
        if (closed) {
          this.cashDrawerSession.set(null);
          this.notification.success(this.i18n.t('pos.cashDrawer.toasts.closed'));
        }
      });
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
