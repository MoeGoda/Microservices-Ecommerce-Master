import { Component, OnInit, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DecimalPipe } from '@angular/common';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { SaleDto } from '../../../shared/models/pos.models';
import { PosService } from '../pos.service';

export interface HeldSalesDialogData {
  locationId: number;
}

// The register's "held sales" picker — every InProgress sale at this
// location (there is no separate "hold" action; StartSaleCommand always
// allowed more than one concurrent InProgress sale, this dialog is just
// the missing list view over that). Closes with the picked SaleDto so
// pos-register.component can load it straight in, the same
// "close with the object, not just an id" pattern the create dialogs use.
@Component({
  selector: 'app-held-sales-dialog',
  imports: [MatButtonModule, MatDialogModule, MatIconModule, MatProgressSpinnerModule, DecimalPipe, TranslatePipe, EmptyStateComponent],
  templateUrl: './held-sales-dialog.component.html',
  styleUrl: './held-sales-dialog.component.scss',
})
export class HeldSalesDialogComponent implements OnInit {
  private readonly data = inject<HeldSalesDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject<MatDialogRef<HeldSalesDialogComponent, SaleDto>>(MatDialogRef);
  private readonly posService = inject(PosService);

  readonly loading = signal(true);
  readonly sales = signal<SaleDto[]>([]);

  ngOnInit(): void {
    this.posService.getInProgressSales(this.data.locationId).subscribe({
      next: (sales) => {
        this.sales.set(sales);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  resume(sale: SaleDto): void {
    this.dialogRef.close(sale);
  }
}
