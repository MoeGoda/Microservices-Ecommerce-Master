import { DecimalPipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize, forkJoin } from 'rxjs';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { SectionHeaderComponent } from '../../../shared/components/section-header/section-header.component';
import { PurchasingService } from '../../purchasing/purchasing.service';
import { WarehouseService } from '../../warehouse/warehouse.service';
import { ReportingService } from '../reporting.service';

// N-C — the former 970-line ReportsDashboardComponent (all 8 report
// widgets rendered on one page) is now the Reports group's landing
// page: one real headline number per report, each linking to that
// report's own routed screen — same "overview page, not straight into
// a list" idea as WarehouseDashboardComponent (M). Every number here is
// real data already served by an existing endpoint (a few — sales
// ledger/stock movement counts — via pageSize=1 just to read
// totalCount cheaply); nothing is fabricated to fill a tile.
@Component({
  selector: 'app-reports-dashboard',
  imports: [
    DecimalPipe,
    RouterLink,
    MatButtonModule,
    MatProgressSpinnerModule,
    PageHeaderComponent,
    SectionHeaderComponent,
    TranslatePipe,
  ],
  templateUrl: './reports-dashboard.component.html',
  styleUrl: './reports-dashboard.component.scss',
})
export class ReportsDashboardComponent implements OnInit {
  readonly loading = signal(false);

  readonly salesTotal = signal(0);
  readonly topSellingName = signal<string | null>(null);
  readonly lowStockCount = signal(0);
  readonly salesCount = signal(0);
  readonly cashierCount = signal(0);
  readonly movementsCount = signal(0);
  readonly inventoryValue = signal(0);
  readonly openAgingCount = signal(0);

  constructor(
    private readonly reportingService: ReportingService,
    private readonly warehouseService: WarehouseService,
    private readonly purchasingService: PurchasingService,
  ) {}

  ngOnInit(): void {
    this.loading.set(true);
    forkJoin({
      salesByDay: this.reportingService.getSalesByDay(),
      topSelling: this.reportingService.getTopSellingItems(1),
      lowStock: this.reportingService.getLowStock(),
      salesLedger: this.reportingService.getSalesLedger(1, 1),
      cashierPerformance: this.reportingService.getCashierPerformance(),
      stockMovements: this.reportingService.getStockMovements(1, 1),
      inventoryValuation: this.warehouseService.getInventoryValuation(),
      aging: this.purchasingService.getPurchaseOrderAging(),
    })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe(({ salesByDay, topSelling, lowStock, salesLedger, cashierPerformance, stockMovements, inventoryValuation, aging }) => {
        this.salesTotal.set(salesByDay.reduce((sum, r) => sum + r.total, 0));
        this.topSellingName.set(topSelling[0]?.itemName ?? null);
        this.lowStockCount.set(lowStock.length);
        this.salesCount.set(salesLedger.totalCount);
        this.cashierCount.set(cashierPerformance.length);
        this.movementsCount.set(stockMovements.totalCount);
        this.inventoryValue.set(inventoryValuation.reduce((sum, l) => sum + l.totalValue, 0));
        this.openAgingCount.set(aging.filter((o) => o.ageDaysSinceOrdered !== null).length);
      });
  }
}
