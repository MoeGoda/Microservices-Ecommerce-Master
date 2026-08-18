import { Component, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { finalize } from 'rxjs';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { FilterPanelComponent } from '../../../shared/components/filter-panel/filter-panel.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { emptyPage, PagedResult } from '../../../shared/models/pagination.models';
import { SupplierDto } from '../../../shared/models/purchasing.models';
import { paginateClientSide } from '../../../shared/utils/paginate-client-side';
import { PurchasingService } from '../purchasing.service';
import { SupplierCreateDialogComponent } from './supplier-create-dialog.component';
import { SupplierDetailDialogComponent } from './supplier-detail-dialog.component';

// L — Suppliers is a plain grid: the create form and the per-row
// detail/activate-deactivate panel both live in dialogs
// (SupplierCreateDialogComponent / SupplierDetailDialogComponent), so
// this component only owns the paged list itself. Suppliers are
// deactivated, never deleted, the same reasoning as Identity's Users
// (H): a Supplier referenced by existing PurchaseOrder history can't be
// removed without orphaning that history.
// M — GetSuppliersQuery has no server-side search (only page/pageSize,
// capped at 100), so the new filter panel searches/filters client-side
// over the fetched batch, same paginateClientSide pattern the new
// Warehouse ledger screens use.
@Component({
  selector: 'app-suppliers-admin',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTooltipModule,
    EmptyStateComponent,
    FilterPanelComponent,
    PageHeaderComponent,
    StatusBadgeComponent,
    TranslatePipe,
  ],
  templateUrl: './suppliers-admin.component.html',
  styleUrl: './suppliers-admin.component.scss',
})
export class SuppliersAdminComponent implements OnInit {
  readonly pagedSuppliers = signal<PagedResult<SupplierDto>>(emptyPage());
  readonly loadingSuppliers = signal(false);

  private allSuppliers: SupplierDto[] = [];

  readonly filterForm = new FormGroup({
    search: new FormControl('', { nonNullable: true }),
    status: new FormControl<'active' | 'inactive' | null>(null),
  });

  constructor(
    private readonly purchasingService: PurchasingService,
    private readonly dialog: MatDialog,
  ) {}

  ngOnInit(): void {
    this.loadSuppliers();
  }

  loadSuppliers(): void {
    this.loadingSuppliers.set(true);
    this.purchasingService
      .getSuppliers(1, 100)
      .pipe(finalize(() => this.loadingSuppliers.set(false)))
      .subscribe({
        next: (result) => {
          this.allSuppliers = result.items;
          this.applyClientFilters(1);
        },
      });
  }

  applyClientFilters(page: number): void {
    const value = this.filterForm.getRawValue();
    const search = value.search.trim().toLowerCase();
    const filtered = this.allSuppliers.filter(
      (s) =>
        (!search || s.name.toLowerCase().includes(search) || (s.contactName ?? '').toLowerCase().includes(search)) &&
        (!value.status || (value.status === 'active' ? s.isActive : !s.isActive)),
    );
    this.pagedSuppliers.set(paginateClientSide(filtered, page, this.pagedSuppliers().pageSize));
  }

  onSearch(): void {
    this.applyClientFilters(1);
  }

  onResetFilters(): void {
    this.filterForm.reset({ search: '', status: null });
    this.applyClientFilters(1);
  }

  onSuppliersPageChange(event: PageEvent): void {
    if (event.pageSize !== this.pagedSuppliers().pageSize) {
      this.pagedSuppliers.update((current) => ({ ...current, pageSize: event.pageSize }));
    }
    this.applyClientFilters(event.pageIndex + 1);
  }

  openCreateDialog(): void {
    this.dialog
      .open(SupplierCreateDialogComponent)
      .afterClosed()
      .subscribe((created) => {
        if (created) {
          this.loadSuppliers();
        }
      });
  }

  openDetailDialog(supplier: SupplierDto): void {
    this.dialog
      .open(SupplierDetailDialogComponent, { data: { supplier } })
      .afterClosed()
      .subscribe((updated) => {
        if (updated) {
          this.allSuppliers = this.allSuppliers.map((s) => (s.id === updated.id ? updated : s));
          this.applyClientFilters(this.pagedSuppliers().page);
        }
      });
  }
}
