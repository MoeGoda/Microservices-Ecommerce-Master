import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize } from 'rxjs';
import { I18nService } from '../../../core/i18n/i18n.service';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { NotificationService } from '../../../core/notifications/notification.service';
import { CashMovementDto } from '../../../shared/models/pos.models';
import { PosService } from '../pos.service';

export interface CashMovementDialogData {
  locationId: number;
  type: 'CashIn' | 'CashOut';
}

// One dialog for both register actions — "Cash In" and "Cash Out" are
// the exact same shape (amount + reason against the currently open
// session), the only difference is which RecordCashMovementCommand.Type
// gets sent, so there is no separate CashOutDialogComponent.
@Component({
  selector: 'app-cash-movement-dialog',
  imports: [ReactiveFormsModule, MatButtonModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatProgressSpinnerModule, TranslatePipe],
  templateUrl: './cash-movement-dialog.component.html',
  styleUrl: './cash-movement-dialog.component.scss',
})
export class CashMovementDialogComponent {
  private readonly data = inject<CashMovementDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject<MatDialogRef<CashMovementDialogComponent, CashMovementDto>>(MatDialogRef);
  private readonly posService = inject(PosService);
  private readonly notification = inject(NotificationService);
  private readonly i18n = inject(I18nService);

  readonly type = this.data.type;
  readonly submitting = signal(false);

  readonly form = new FormGroup({
    amount: new FormControl<number | null>(null, { validators: [Validators.required, Validators.min(0.01)] }),
    reason: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  submit(): void {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.submitting.set(true);
    this.posService
      .recordCashMovement(this.data.locationId, this.type, value.amount!, value.reason)
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: (movement) => {
          this.notification.success(this.i18n.t(this.type === 'CashIn' ? 'pos.cashDrawer.toasts.cashInRecorded' : 'pos.cashDrawer.toasts.cashOutRecorded'));
          this.dialogRef.close(movement);
        },
      });
  }
}
