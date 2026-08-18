import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';

// M — a generic yes/no dialog for destructive-ish actions (cancel a PO,
// cancel an adjustment, ...) so those flows don't each need their own
// bespoke confirm markup. Follows the exact `inject()` +
// close-with-payload MatDialog convention already established by the
// Phase-L detail dialogs (e.g. supplier-detail-dialog.component.ts) —
// closes with `true` on confirm, `false`/undefined otherwise. Title and
// message are already-translated strings, same as PageHeaderComponent.
export interface ConfirmDialogData {
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  danger?: boolean;
}

@Component({
  selector: 'app-confirm-dialog',
  imports: [MatButtonModule, MatDialogModule],
  templateUrl: './confirm-dialog.component.html',
  styleUrl: './confirm-dialog.component.scss',
})
export class ConfirmDialogComponent {
  private readonly data = inject<ConfirmDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject<MatDialogRef<ConfirmDialogComponent, boolean>>(MatDialogRef);

  readonly title = this.data.title;
  readonly message = this.data.message;
  readonly confirmLabel = this.data.confirmLabel ?? 'Confirm';
  readonly cancelLabel = this.data.cancelLabel ?? 'Cancel';
  readonly danger = this.data.danger ?? false;

  confirm(): void {
    this.dialogRef.close(true);
  }

  cancel(): void {
    this.dialogRef.close(false);
  }
}
