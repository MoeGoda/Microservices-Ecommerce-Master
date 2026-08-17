import { Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { finalize } from 'rxjs';
import { I18nService } from '../../../core/i18n/i18n.service';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { NotificationService } from '../../../core/notifications/notification.service';
import { SupplierDto } from '../../../shared/models/purchasing.models';
import { PurchasingService } from '../purchasing.service';

export interface SupplierDetailDialogData {
  supplier: SupplierDto;
}

// L — the "click a row's action button, see everything about it in a
// popup" pattern requested for this screen. Toggling active/inactive
// updates the dialog's own view immediately, then closes with the
// updated SupplierDto so the grid row behind it refreshes without a
// second fetch.
@Component({
  selector: 'app-supplier-detail-dialog',
  imports: [MatButtonModule, MatDialogModule, TranslatePipe],
  templateUrl: './supplier-detail-dialog.component.html',
  styleUrl: './supplier-detail-dialog.component.scss',
})
export class SupplierDetailDialogComponent {
  private readonly data = inject<SupplierDetailDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject<MatDialogRef<SupplierDetailDialogComponent, SupplierDto>>(MatDialogRef);
  private readonly purchasingService = inject(PurchasingService);
  private readonly notification = inject(NotificationService);
  private readonly i18n = inject(I18nService);

  readonly supplier = signal<SupplierDto>(this.data.supplier);
  readonly togglingActive = signal(false);

  toggleActive(): void {
    if (this.togglingActive()) {
      return;
    }

    const nextActive = !this.supplier().isActive;
    this.togglingActive.set(true);
    this.purchasingService
      .setSupplierActive(this.supplier().id, nextActive)
      .pipe(finalize(() => this.togglingActive.set(false)))
      .subscribe({
        next: (updated) => {
          this.notification.success(this.i18n.t(nextActive ? 'suppliers.toasts.activated' : 'suppliers.toasts.deactivated', { name: updated.name }));
          this.supplier.set(updated);
          this.dialogRef.close(updated);
        },
      });
  }
}
