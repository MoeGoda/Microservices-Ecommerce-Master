import { Component, signal } from '@angular/core';
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
import { SupplierDto } from '../../../shared/models/purchasing.models';
import { PurchasingService } from '../purchasing.service';

// L — the create half of what used to be an inline card above the
// Suppliers grid. Closing with the created SupplierDto (rather than just
// `true`) lets the grid append/refresh without a second round trip.
@Component({
  selector: 'app-supplier-create-dialog',
  imports: [ReactiveFormsModule, MatButtonModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatProgressSpinnerModule, TranslatePipe],
  templateUrl: './supplier-create-dialog.component.html',
  styleUrl: './supplier-create-dialog.component.scss',
})
export class SupplierCreateDialogComponent {
  readonly creatingSupplier = signal(false);

  readonly createForm = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    contactName: new FormControl('', { nonNullable: true }),
    email: new FormControl('', { nonNullable: true, validators: [Validators.email] }),
    phone: new FormControl('', { nonNullable: true }),
    address: new FormControl('', { nonNullable: true }),
  });

  constructor(
    private readonly dialogRef: MatDialogRef<SupplierCreateDialogComponent, SupplierDto>,
    private readonly purchasingService: PurchasingService,
    private readonly notification: NotificationService,
    private readonly i18n: I18nService,
  ) {}

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
          this.dialogRef.close(created);
        },
      });
  }
}
