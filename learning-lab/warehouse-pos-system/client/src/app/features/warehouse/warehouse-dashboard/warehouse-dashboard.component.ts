import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize, forkJoin } from 'rxjs';
import { I18nService } from '../../../core/i18n/i18n.service';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { PurchaseOrderAgingLineDto } from '../../../shared/models/purchasing.models';
import { StockLevelRecordDto, StockMovementRecordDto } from '../../../shared/models/reporting.models';
import { PurchasingService } from '../../purchasing/purchasing.service';
import { ReportingService } from '../../reporting/reporting.service';
import { WarehouseService } from '../warehouse.service';

// M — the Warehouse group's landing page. There is no single backend
// query for "a warehouse dashboard" — this assembles the same four
// calls the combined sales+warehouse /reports screen already makes
// (getLowStock, getInventoryValuation, getPurchaseOrderAging,
// getStockMovements), scoped to warehouse-only content, so the
// Warehouse section has a real overview page instead of dropping
// straight into a list. /reports stays as-is for the fuller
// sales-inclusive view.
@Component({
  selector: 'app-warehouse-dashboard',
  imports: [
    DatePipe,
    DecimalPipe,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressSpinnerModule,
    EmptyStateComponent,
    PageHeaderComponent,
    StatusBadgeComponent,
    TranslatePipe,
  ],
  templateUrl: './warehouse-dashboard.component.html',
  styleUrl: './warehouse-dashboard.component.scss',
})
export class WarehouseDashboardComponent implements OnInit {
  readonly loading = signal(false);
  readonly lowStock = signal<StockLevelRecordDto[]>([]);
  readonly inventoryItemCount = signal(0);
  readonly inventoryTotalValue = signal(0);
  readonly agingOrders = signal<PurchaseOrderAgingLineDto[]>([]);
  readonly recentMovements = signal<StockMovementRecordDto[]>([]);

  constructor(
    private readonly reportingService: ReportingService,
    private readonly warehouseService: WarehouseService,
    private readonly purchasingService: PurchasingService,
    private readonly i18n: I18nService,
  ) {}

  ngOnInit(): void {
    this.loading.set(true);
    forkJoin({
      lowStock: this.reportingService.getLowStock(),
      inventory: this.warehouseService.getInventoryValuation(),
      aging: this.purchasingService.getPurchaseOrderAging(),
      movements: this.reportingService.getStockMovements(1, 10),
    })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe(({ lowStock, inventory, aging, movements }) => {
        this.lowStock.set(lowStock.slice(0, 5));
        this.inventoryItemCount.set(inventory.length);
        this.inventoryTotalValue.set(inventory.reduce((sum, line) => sum + line.totalValue, 0));
        this.agingOrders.set(
          aging
            .filter((o) => o.ageDaysSinceOrdered != null)
            .sort((a, b) => (b.ageDaysSinceOrdered ?? 0) - (a.ageDaysSinceOrdered ?? 0))
            .slice(0, 5),
        );
        this.recentMovements.set(movements.items.slice(0, 10));
      });
  }

  reasonLabel(reason: string): string {
    return this.i18n.t('stockMovements.reason.' + reason);
  }
}
