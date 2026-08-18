import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { StockMovementRecordDto } from '../../models/reporting.models';
import { StatusBadgeComponent } from '../status-badge/status-badge.component';

export interface StockMovementDetailDialogData {
  movement: StockMovementRecordDto;
  reasonLabel: string;
}

// M — Receipts/Transfers/Adjustments/Issues are all read-only views over
// the same StockMovementRecordDto ledger (Reporting's stock-movements
// query), filtered by reason/sign. A ledger row has no id of its own and
// nothing further to fetch — there is no valid Edit/Delete/Approve
// action for an immutable transaction record — so "View" (restating the
// row in a dialog) is the one row action that's actually real, shared
// once here instead of once per screen.
@Component({
  selector: 'app-stock-movement-detail-dialog',
  imports: [DatePipe, DecimalPipe, MatButtonModule, MatDialogModule, StatusBadgeComponent, TranslatePipe],
  templateUrl: './stock-movement-detail-dialog.component.html',
  styleUrl: './stock-movement-detail-dialog.component.scss',
})
export class StockMovementDetailDialogComponent {
  private readonly data = inject<StockMovementDetailDialogData>(MAT_DIALOG_DATA);

  readonly movement = this.data.movement;
  readonly reasonLabel = this.data.reasonLabel;
}
