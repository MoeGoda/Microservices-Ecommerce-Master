import { Component, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize } from 'rxjs';
import { I18nService } from '../../core/i18n/i18n.service';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { NotificationService } from '../../core/notifications/notification.service';
import { emptyPage, PagedResult } from '../../shared/models/pagination.models';
import { SupplierDto } from '../../shared/models/purchasing.models';
import { PurchasingService } from './purchasing.service';

// I — Suppliers are deactivated, never deleted, the same reasoning as
// Identity's Users (H): a Supplier referenced by existing PurchaseOrder
// history can't be removed without orphaning that history.
@Component({
  selector: 'app-suppliers-admin',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    TranslatePipe,
  ],
  templateUrl: './suppliers-admin.component.html',
  styleUrl: './suppliers-admin.component.scss',
})
export class SuppliersAdminComponent implements OnInit {
  readonly pagedSuppliers = signal<PagedResult<SupplierDto>>(emptyPage());
  readonly loadingSuppliers = signal(false);
  readonly creatingSupplier = signal(false);
  readonly togglingSupplierId = signal<number | null>(null);

  readonly createForm = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    contactName: new FormControl('', { nonNullable: true }),
    email: new FormControl('', { nonNullable: true, validators: [Validators.email] }),
    phone: new FormControl('', { nonNullable: true }),
    address: new FormControl('', { nonNullable: true }),
  });

  constructor(
    private readonly purchasingService: PurchasingService,
    private readonly notification: NotificationService,
    private readonly i18n: I18nService,
  ) {}

  ngOnInit(): void {
    this.loadSuppliers();
  }

  loadSuppliers(page = 1): void {
    this.loadingSuppliers.set(true);
    this.purchasingService
      .getSuppliers(page, this.pagedSuppliers().pageSize)
      .pipe(finalize(() => this.loadingSuppliers.set(false)))
      .subscribe({ next: (result) => this.pagedSuppliers.set(result) });
  }

  onSuppliersPageChange(event: PageEvent): void {
    if (event.pageSize !== this.pagedSuppliers().pageSize) {
      this.pagedSuppliers.update((current) => ({ ...current, pageSize: event.pageSize }));
    }
    this.loadSuppliers(event.pageIndex + 1);
  }

  submitCreate(): void {
    if (this.createForm.invalid || this.creatingSupplier()) {
      this.createForm.markAllAsTouched();
      return;
    }

    const value = this.createForm.getRawValue();
    this.creatingSupplier.set(true);
    this.purchasingService
      .createSupplier(value)
      .pipe(finalize(() => this.creatingSupplier.set(false)))
      .subscribe({
        next: (created) => {
          this.notification.success(this.i18n.t('suppliers.toasts.created', { name: created.name }));
          this.createForm.reset({ name: '', contactName: '', email: '', phone: '', address: '' });
          this.loadSuppliers();
        },
      });
  }

  toggleActive(supplier: SupplierDto): void {
    if (this.togglingSupplierId()) {
      return;
    }

    const nextActive = !supplier.isActive;
    this.togglingSupplierId.set(supplier.id);
    this.purchasingService
      .setSupplierActive(supplier.id, nextActive)
      .pipe(finalize(() => this.togglingSupplierId.set(null)))
      .subscribe({
        next: (updated) => {
          this.notification.success(this.i18n.t(nextActive ? 'suppliers.toasts.activated' : 'suppliers.toasts.deactivated', { name: updated.name }));
          this.pagedSuppliers.update((current) => ({
            ...current,
            items: current.items.map((s) => (s.id === updated.id ? updated : s)),
          }));
        },
      });
  }
}
