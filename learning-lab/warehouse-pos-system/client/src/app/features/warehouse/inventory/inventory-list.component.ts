import { DecimalPipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
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
import { CategoryDto, InventoryValuationLineDto } from '../../../shared/models/warehouse.models';
import { paginateClientSide } from '../../../shared/utils/paginate-client-side';
import { WarehouseService } from '../warehouse.service';

// M — "Inventory": the requested cross-item, cross-location on-hand
// view. Warehouse.API has no single query for that shape, but
// GetInventoryValuationQuery already returns exactly the rows this
// screen needs (per-item quantity on hand + value, aggregated across
// locations) — the same call the combined /reports dashboard already
// makes. No backend change; this just gives that data its own
// searchable/filterable screen instead of being buried inside a wider
// sales+warehouse dashboard. Per-row "View" navigates to the item's own
// /items/:id page rather than opening a dialog — that's the real,
// existing screen for everything about one item, not a new one to
// duplicate it.
@Component({
  selector: 'app-inventory-list',
  imports: [
    DecimalPipe,
    ReactiveFormsModule,
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
  templateUrl: './inventory-list.component.html',
  styleUrl: './inventory-list.component.scss',
})
export class InventoryListComponent implements OnInit {
  readonly pagedLines = signal<PagedResult<InventoryValuationLineDto>>(emptyPage());
  readonly loading = signal(false);
  readonly categories = signal<CategoryDto[]>([]);

  private allLines: InventoryValuationLineDto[] = [];

  readonly filterForm = new FormGroup({
    search: new FormControl('', { nonNullable: true }),
    categoryName: new FormControl<string | null>(null),
  });

  readonly categoryLabel = (category: CategoryDto): string => category.name;
  readonly categoryValue = (category: CategoryDto): string => category.name;

  constructor(
    private readonly warehouseService: WarehouseService,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    this.warehouseService.getCategories().subscribe((categories) => this.categories.set(categories));
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.warehouseService
      .getInventoryValuation()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((lines) => {
        this.allLines = lines;
        this.applyClientFilters(1);
      });
  }

  applyClientFilters(page: number): void {
    const value = this.filterForm.getRawValue();
    const search = value.search.trim().toLowerCase();
    const filtered = this.allLines.filter(
      (line) =>
        (!search || line.sku.toLowerCase().includes(search) || line.itemName.toLowerCase().includes(search)) &&
        (!value.categoryName || line.categoryName === value.categoryName),
    );
    this.pagedLines.set(paginateClientSide(filtered, page, this.pagedLines().pageSize));
  }

  onSearch(): void {
    this.applyClientFilters(1);
  }

  onResetFilters(): void {
    this.filterForm.reset({ search: '', categoryName: null });
    this.applyClientFilters(1);
  }

  onPageChange(event: PageEvent): void {
    if (event.pageSize !== this.pagedLines().pageSize) {
      this.pagedLines.update((current) => ({ ...current, pageSize: event.pageSize }));
    }
    this.applyClientFilters(event.pageIndex + 1);
  }

  viewItem(line: InventoryValuationLineDto): void {
    this.router.navigate(['/items', line.itemId]);
  }
}
