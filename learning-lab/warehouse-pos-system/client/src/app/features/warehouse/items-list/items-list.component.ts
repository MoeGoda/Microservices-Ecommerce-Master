import { DecimalPipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize } from 'rxjs';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { emptyPage, PagedResult } from '../../../shared/models/pagination.models';
import { ItemSummaryDto } from '../../../shared/models/warehouse.models';
import { WarehouseService } from '../warehouse.service';

// K — the browse/search half of what used to be one long
// ItemsAdminComponent screen (create form + list + detail management all
// on one page). This one only lists; creating is its own route
// (/items/new) and managing one item is its own route (/items/:id) —
// three separate, routed screens instead of a single page with an
// in-page "selection panel," the split the redesign asked for.
@Component({
  selector: 'app-items-list',
  imports: [DecimalPipe, RouterLink, MatButtonModule, MatCardModule, MatIconModule, MatPaginatorModule, MatProgressSpinnerModule, TranslatePipe],
  templateUrl: './items-list.component.html',
  styleUrl: './items-list.component.scss',
})
export class ItemsListComponent implements OnInit {
  readonly pagedItems = signal<PagedResult<ItemSummaryDto>>(emptyPage());
  readonly loadingItems = signal(false);

  constructor(private readonly warehouseService: WarehouseService) {}

  ngOnInit(): void {
    this.loadItems();
  }

  loadItems(page = 1): void {
    this.loadingItems.set(true);
    this.warehouseService
      .getItems(page, this.pagedItems().pageSize)
      .pipe(finalize(() => this.loadingItems.set(false)))
      .subscribe({ next: (result) => this.pagedItems.set(result) });
  }

  onItemsPageChange(event: PageEvent): void {
    // MatPaginator's pageIndex is 0-based; the backend's Page is 1-based.
    if (event.pageSize !== this.pagedItems().pageSize) {
      this.pagedItems.update((current) => ({ ...current, pageSize: event.pageSize }));
    }
    this.loadItems(event.pageIndex + 1);
  }
}
