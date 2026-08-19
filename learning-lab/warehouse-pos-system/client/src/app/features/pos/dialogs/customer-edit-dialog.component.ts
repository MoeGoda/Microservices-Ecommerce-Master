import { DecimalPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize } from 'rxjs';
import { I18nService } from '../../../core/i18n/i18n.service';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { NotificationService } from '../../../core/notifications/notification.service';
import { CustomerDto } from '../../../shared/models/pos.models';
import { CustomersService } from '../customers.service';

export interface CustomerEditDialogData {
  customer?: CustomerDto;
}

// One dialog for both create and edit — unlike Suppliers' separate
// create/detail dialogs, a POS customer has nothing status-like to
// toggle; the only thing an existing customer's dialog adds over a new
// one is a read-only loyalty/balance summary and the balance-adjustment
// action below.
@Component({
  selector: 'app-customer-edit-dialog',
  imports: [
    ReactiveFormsModule,
    DecimalPipe,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    TranslatePipe,
  ],
  templateUrl: './customer-edit-dialog.component.html',
  styleUrl: './customer-edit-dialog.component.scss',
})
export class CustomerEditDialogComponent {
  private readonly data = inject<CustomerEditDialogData>(MAT_DIALOG_DATA, { optional: true }) ?? {};
  private readonly dialogRef = inject<MatDialogRef<CustomerEditDialogComponent, CustomerDto>>(MatDialogRef);
  private readonly customersService = inject(CustomersService);
  private readonly notification = inject(NotificationService);
  private readonly i18n = inject(I18nService);

  readonly existing = signal<CustomerDto | null>(this.data.customer ?? null);
  readonly saving = signal(false);
  readonly adjustingBalance = signal(false);
  readonly showBalanceAdjustment = signal(false);

  readonly form = new FormGroup({
    name: new FormControl(this.data.customer?.name ?? '', { nonNullable: true, validators: [Validators.required] }),
    phone: new FormControl(this.data.customer?.phone ?? '', { nonNullable: true }),
    email: new FormControl(this.data.customer?.email ?? '', { nonNullable: true, validators: [Validators.email] }),
  });

  readonly balanceForm = new FormGroup({
    delta: new FormControl(0, { nonNullable: true, validators: [Validators.required] }),
    reason: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  submit(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const request = { name: value.name, phone: value.phone || null, email: value.email || null };
    this.saving.set(true);

    const existing = this.existing();
    const call = existing ? this.customersService.update(existing.id, request) : this.customersService.create(request);
    call.pipe(finalize(() => this.saving.set(false))).subscribe({
      next: (saved) => {
        this.notification.success(this.i18n.t(existing ? 'pos.customers.toasts.updated' : 'pos.customers.toasts.created', { name: saved.name }));
        this.dialogRef.close(saved);
      },
    });
  }

  submitBalanceAdjustment(): void {
    const existing = this.existing();
    if (!existing || this.balanceForm.invalid || this.adjustingBalance()) {
      this.balanceForm.markAllAsTouched();
      return;
    }

    const value = this.balanceForm.getRawValue();
    this.adjustingBalance.set(true);
    this.customersService
      .adjustBalance(existing.id, { delta: value.delta, reason: value.reason })
      .pipe(finalize(() => this.adjustingBalance.set(false)))
      .subscribe({
        next: (updated) => {
          this.notification.success(this.i18n.t('pos.customers.toasts.balanceAdjusted'));
          this.existing.set(updated);
          this.balanceForm.reset({ delta: 0, reason: '' });
          this.showBalanceAdjustment.set(false);
        },
      });
  }
}
