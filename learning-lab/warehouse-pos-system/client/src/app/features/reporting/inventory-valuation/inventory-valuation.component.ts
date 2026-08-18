import { DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize } from 'rxjs';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { InventoryValuationLineDto } from '../../../shared/models/warehouse.models';
import { WarehouseService } from '../../warehouse/warehouse.service';

// N-C — split out of the former ReportsDashboardComponent verbatim. A
// live current-state snapshot, no date axis to filter on — same as the
// monolith had it.
@Component({
  selector: 'app-inventory-valuation',
  imports: [DecimalPipe, MatCardModule, MatProgressSpinnerModule, PageHeaderComponent, TranslatePipe],
  templateUrl: './inventory-valuation.component.html',
  styleUrl: './inventory-valuation.component.scss',
})
export class InventoryValuationComponent implements OnInit {
  readonly loading = signal(false);
  readonly inventoryValuation = signal<InventoryValuationLineDto[]>([]);

  readonly inventoryValuationTotal = computed(() => this.inventoryValuation().reduce((sum, l) => sum + l.totalValue, 0));

  constructor(private readonly warehouseService: WarehouseService) {}

  ngOnInit(): void {
    this.loading.set(true);
    this.warehouseService
      .getInventoryValuation()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((lines) => this.inventoryValuation.set(lines));
  }
}
