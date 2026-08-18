import { DecimalPipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize } from 'rxjs';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { PurchaseOrderAgingLineDto } from '../../../shared/models/purchasing.models';
import { PurchasingService } from '../../purchasing/purchasing.service';

// N-C — split out of the former ReportsDashboardComponent verbatim. A
// live current-state snapshot, no date axis to filter on.
@Component({
  selector: 'app-purchase-order-aging',
  imports: [DecimalPipe, MatCardModule, MatIconModule, MatProgressSpinnerModule, PageHeaderComponent, TranslatePipe],
  templateUrl: './purchase-order-aging.component.html',
  styleUrl: './purchase-order-aging.component.scss',
})
export class PurchaseOrderAgingComponent implements OnInit {
  readonly loading = signal(false);
  readonly purchaseOrderAging = signal<PurchaseOrderAgingLineDto[]>([]);

  constructor(private readonly purchasingService: PurchasingService) {}

  ngOnInit(): void {
    this.loading.set(true);
    this.purchasingService
      .getPurchaseOrderAging()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((lines) => this.purchaseOrderAging.set(lines));
  }

  // Only Ordered/PartiallyReceived orders carry an age at all — see
  // PurchaseOrderAgingLineDto's own comment.
  openPurchaseOrderAging(): PurchaseOrderAgingLineDto[] {
    return this.purchaseOrderAging().filter((o) => o.ageDaysSinceOrdered !== null);
  }
}
