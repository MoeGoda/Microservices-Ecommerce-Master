import { Component, OnInit, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { CashDrawerXReportDto } from '../../../shared/models/pos.models';
import { PosService } from '../pos.service';

export interface XReportDialogData {
  sessionId: number;
}

// A read-only mid-shift snapshot — see CashDrawerXReportDto's own backend
// comment for why SalesTotal is shown separately from
// ExpectedCashInDrawer rather than folded into it: there is no
// payment-method field anywhere in this app, so there's no way to know
// how much of SalesTotal was actually cash. Showing it as "expected
// cash" would be a fabricated number.
@Component({
  selector: 'app-x-report-dialog',
  imports: [MatButtonModule, MatDialogModule, MatProgressSpinnerModule, DecimalPipe, TranslatePipe],
  templateUrl: './x-report-dialog.component.html',
  styleUrl: './x-report-dialog.component.scss',
})
export class XReportDialogComponent implements OnInit {
  private readonly data = inject<XReportDialogData>(MAT_DIALOG_DATA);
  private readonly posService = inject(PosService);

  readonly loading = signal(true);
  readonly report = signal<CashDrawerXReportDto | null>(null);

  ngOnInit(): void {
    this.posService.getCashDrawerXReport(this.data.sessionId).subscribe({
      next: (report) => {
        this.report.set(report);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
