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

export interface OpenCashDrawerDialogData {
  locationId: number;
}

@Component({
  selector: 'app-open-cash-drawer-dialog',
  imports: [ReactiveFormsModule, MatButtonModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatProgressSpinnerModule, TranslatePipe],
  templateUrl: './open-cash-drawer-dialog.component.html',
  styleUrl: './open-cash-drawer-dialog.component.scss',
})
export class OpenCashDrawerDialogComponent {
  private readonly data = inject<OpenCashDrawerDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject<MatDialogRef<OpenCashDrawerDialogComponent, CashDrawerSessionDto>>(MatDialogRef);
  private readonly posService = inject(PosService);

  readonly submitting = signal(false);

  readonly form = new FormGroup({
    openingFloat: new FormControl<number | null>(0, { validators: [Validators.required, Validators.min(0)] }),
  });

  submit(): void {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.posService
      .openCashDrawer(this.data.locationId, this.form.getRawValue().openingFloat!)
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({ next: (session) => this.dialogRef.close(session) });
  }
}
