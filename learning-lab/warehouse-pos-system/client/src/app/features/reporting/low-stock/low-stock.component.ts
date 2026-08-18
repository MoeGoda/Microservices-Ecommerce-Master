import { Component, OnInit, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize } from 'rxjs';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StockLevelRecordDto } from '../../../shared/models/reporting.models';
import { ReportingService } from '../reporting.service';

// N-C — split out of the former ReportsDashboardComponent verbatim.
@Component({
  selector: 'app-low-stock',
  imports: [MatCardModule, MatIconModule, MatProgressSpinnerModule, PageHeaderComponent, TranslatePipe],
  templateUrl: './low-stock.component.html',
  styleUrl: './low-stock.component.scss',
})
export class LowStockComponent implements OnInit {
  readonly loading = signal(false);
  readonly lowStock = signal<StockLevelRecordDto[]>([]);

  constructor(private readonly reportingService: ReportingService) {}

  ngOnInit(): void {
    this.loading.set(true);
    this.reportingService
      .getLowStock()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((rows) => this.lowStock.set(rows));
  }

  // Every row GetLowStock returns is already at/below its own threshold —
  // the split here is just how far below: zero on hand is a harder stop
  // (can't sell it at all) than merely-below-threshold, so it earns the
  // more severe status step. Never color alone (palette.md) — the icon and
  // the "Out of stock"/"Low stock" text always ride along with it.
  stockSeverity(row: StockLevelRecordDto): 'critical' | 'warning' {
    return row.quantityOnHand <= 0 ? 'critical' : 'warning';
  }
}
