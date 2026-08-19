import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize } from 'rxjs';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { CashDrawerSessionDto } from '../../../shared/models/pos.models';
import { PosService } from '../pos.service';

export interface CloseCashDrawerDialogData {
  sessionId: number;
}

// The counted-cash entry that ends a shift — separate from the X report
// (a read-only mid-shift snapshot that never closes anything); this is
// the Z-close.
@Component({
  selector: 'app-close-cash-drawer-dialog',
  imports: [ReactiveFormsModule, MatButtonModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatProgressSpinnerModule, TranslatePipe],
  templateUrl: './close-cash-drawer-dialog.component.html',
  styleUrl: './close-cash-drawer-dialog.component.scss',
})
export class CloseCashDrawerDialogComponent {
  private readonly data = inject<CloseCashDrawerDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject<MatDialogRef<CloseCashDrawerDialogComponent, CashDrawerSessionDto>>(MatDialogRef);
  private readonly posService = inject(PosService);

  readonly submitting = signal(false);

  readonly form = new FormGroup({
    closingCount: new FormControl<number | null>(null, { validators: [Validators.required, Validators.min(0)] }),
  });

  submit(): void {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.posService
      .closeCashDrawer(this.data.sessionId, this.form.getRawValue().closingCount!)
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({ next: (session) => this.dialogRef.close(session) });
  }
}
