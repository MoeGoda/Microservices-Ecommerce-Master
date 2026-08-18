import { DecimalPipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { finalize } from 'rxjs';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { FilterPanelComponent } from '../../../shared/components/filter-panel/filter-panel.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { SearchableSelectComponent } from '../../../shared/components/searchable-select/searchable-select.component';
import { emptyPage, PagedResult } from '../../../shared/models/pagination.models';
import { CategoryDto, ItemSummaryDto } from '../../../shared/models/warehouse.models';
import { paginateClientSide } from '../../../shared/utils/paginate-client-side';
import { WarehouseService } from '../warehouse.service';

// K — the browse/search half of what used to be one long
// ItemsAdminComponent screen (create form + list + detail management all
// on one page). This one only lists; creating is its own route
// (/items/new) and managing one item is its own route (/items/:id) —
// three separate, routed screens instead of a single page with an
// in-page "selection panel," the split the redesign asked for.
// M — GetAllItemsQuery has no search/category filter server-side (only
// page/pageSize, capped at 100) — same limitation the new Warehouse
// ledger screens already work around: fetch up to the 100-item cap,
// then filter/paginate client-side (paginateClientSide) rather than
// re-fetching per keystroke.
@Component({
  selector: 'app-items-list',
  imports: [
    DecimalPipe,
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    EmptyStateComponent,
    FilterPanelComponent,
    PageHeaderComponent,
    SearchableSelectComponent,
    TranslatePipe,
  ],
  templateUrl: './items-list.component.html',
  styleUrl: './items-list.component.scss',
})
export class ItemsListComponent implements OnInit {
  readonly pagedItems = signal<PagedResult<ItemSummaryDto>>(emptyPage());
  readonly loadingItems = signal(false);
  readonly categories = signal<CategoryDto[]>([]);

  private allItems: ItemSummaryDto[] = [];

  readonly filterForm = new FormGroup({
    search: new FormControl('', { nonNullable: true }),
    categoryName: new FormControl<string | null>(null),
  });

  readonly categoryLabel = (category: CategoryDto): string => category.name;
  readonly categoryValue = (category: CategoryDto): string => category.name;

  constructor(private readonly warehouseService: WarehouseService) {}

  ngOnInit(): void {
    this.warehouseService.getCategories().subscribe((categories) => this.categories.set(categories));
    this.loadItems();
  }

  loadItems(): void {
    this.loadingItems.set(true);
    this.warehouseService
      .getItems(1, 100)
      .pipe(finalize(() => this.loadingItems.set(false)))
      .subscribe({
        next: (result) => {
          this.allItems = result.items;
          this.applyClientFilters(1);
        },
      });
  }

  applyClientFilters(page: number): void {
    const value = this.filterForm.getRawValue();
    const search = value.search.trim().toLowerCase();
    const filtered = this.allItems.filter(
      (item) =>
        (!search || item.sku.toLowerCase().includes(search) || item.name.toLowerCase().includes(search)) &&
        (!value.categoryName || item.categoryName === value.categoryName),
    );
    this.pagedItems.set(paginateClientSide(filtered, page, this.pagedItems().pageSize));
  }

  onSearch(): void {
    this.applyClientFilters(1);
  }

  onResetFilters(): void {
    this.filterForm.reset({ search: '', categoryName: null });
    this.applyClientFilters(1);
  }

  onItemsPageChange(event: PageEvent): void {
    // MatPaginator's pageIndex is 0-based; the backend's Page is 1-based.
    if (event.pageSize !== this.pagedItems().pageSize) {
      this.pagedItems.update((current) => ({ ...current, pageSize: event.pageSize }));
    }
    this.applyClientFilters(event.pageIndex + 1);
  }
}
