import { Component, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize } from 'rxjs';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { emptyPage, PagedResult } from '../../../shared/models/pagination.models';
import { SupplierDto } from '../../../shared/models/purchasing.models';
import { PurchasingService } from '../purchasing.service';
import { SupplierCreateDialogComponent } from './supplier-create-dialog.component';
import { SupplierDetailDialogComponent } from './supplier-detail-dialog.component';

// L — Suppliers is now a plain grid: the create form and the per-row
// detail/activate-deactivate panel both moved into dialogs
// (SupplierCreateDialogComponent / SupplierDetailDialogComponent), so
// this component only owns the paged list itself. Suppliers are
// deactivated, never deleted, the same reasoning as Identity's Users
// (H): a Supplier referenced by existing PurchaseOrder history can't be
// removed without orphaning that history.
@Component({
  selector: 'app-suppliers-admin',
  imports: [MatButtonModule, MatCardModule, MatDialogModule, MatIconModule, MatPaginatorModule, MatProgressSpinnerModule, TranslatePipe],
  templateUrl: './suppliers-admin.component.html',
  styleUrl: './suppliers-admin.component.scss',
})
export class SuppliersAdminComponent implements OnInit {
  readonly pagedSuppliers = signal<PagedResult<SupplierDto>>(emptyPage());
  readonly loadingSuppliers = signal(false);

  constructor(
    private readonly purchasingService: PurchasingService,
    private readonly dialog: MatDialog,
  ) {}

  ngOnInit(): void {
    this.loadSuppliers();
  }

  loadSuppliers(page = 1): void {
    this.loadingSuppliers.set(true);
    this.purchasingService
      .getSuppliers(page, this.pagedSuppliers().pageSize)
      .pipe(finalize(() => this.loadingSuppliers.set(false)))
      .subscribe({ next: (result) => this.pagedSuppliers.set(result) });
  }

  onSuppliersPageChange(event: PageEvent): void {
    if (event.pageSize !== this.pagedSuppliers().pageSize) {
      this.pagedSuppliers.update((current) => ({ ...current, pageSize: event.pageSize }));
    }
    this.loadSuppliers(event.pageIndex + 1);
  }

  openCreateDialog(): void {
    this.dialog
      .open(SupplierCreateDialogComponent)
      .afterClosed()
      .subscribe((created) => {
        if (created) {
          this.loadSuppliers(this.pagedSuppliers().page);
        }
      });
  }

  openDetailDialog(supplier: SupplierDto): void {
    this.dialog
      .open(SupplierDetailDialogComponent, { data: { supplier } })
      .afterClosed()
      .subscribe((updated) => {
        if (updated) {
          this.pagedSuppliers.update((current) => ({
            ...current,
            items: current.items.map((s) => (s.id === updated.id ? updated : s)),
          }));
        }
      });
  }
}
