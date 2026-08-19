import { DecimalPipe } from '@angular/common';
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
import { MatTooltipModule } from '@angular/material/tooltip';
import { finalize } from 'rxjs';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { FilterPanelComponent } from '../../../shared/components/filter-panel/filter-panel.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { emptyPage, PagedResult } from '../../../shared/models/pagination.models';
import { CustomerDto } from '../../../shared/models/pos.models';
import { CustomersService } from '../customers.service';
import { CustomerEditDialogComponent } from '../dialogs/customer-edit-dialog.component';

// T9 — a flat admin grid over SearchCustomersQuery, the one real paged
// backend search this app has (unlike Suppliers' client-side-only
// filter over a fetched batch): the search field below hits the server
// on every submit, not a locally cached array.
@Component({
  selector: 'app-customers-admin',
  imports: [
    ReactiveFormsModule,
    DecimalPipe,
    MatButtonModule,
    MatCardModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    EmptyStateComponent,
    FilterPanelComponent,
    PageHeaderComponent,
    TranslatePipe,
  ],
  templateUrl: './customers-admin.component.html',
  styleUrl: './customers-admin.component.scss',
})
export class CustomersAdminComponent implements OnInit {
  readonly pagedCustomers = signal<PagedResult<CustomerDto>>(emptyPage());
  readonly loadingCustomers = signal(false);

  readonly filterForm = new FormGroup({
    search: new FormControl('', { nonNullable: true }),
  });

  constructor(
    private readonly customersService: CustomersService,
    private readonly dialog: MatDialog,
  ) {}

  ngOnInit(): void {
    this.loadCustomers(1);
  }

  loadCustomers(page: number): void {
    const search = this.filterForm.getRawValue().search.trim() || null;
    this.loadingCustomers.set(true);
    this.customersService
      .search(search, page, this.pagedCustomers().pageSize)
      .pipe(finalize(() => this.loadingCustomers.set(false)))
      .subscribe({ next: (result) => this.pagedCustomers.set(result) });
  }

  onSearch(): void {
    this.loadCustomers(1);
  }

  onResetFilters(): void {
    this.filterForm.reset({ search: '' });
    this.loadCustomers(1);
  }

  onCustomersPageChange(event: PageEvent): void {
    if (event.pageSize !== this.pagedCustomers().pageSize) {
      this.pagedCustomers.update((current) => ({ ...current, pageSize: event.pageSize }));
    }
    this.loadCustomers(event.pageIndex + 1);
  }

  openCreateDialog(): void {
    this.dialog
      .open(CustomerEditDialogComponent)
      .afterClosed()
      .subscribe((created: CustomerDto | undefined) => {
        if (created) {
          this.loadCustomers(1);
        }
      });
  }

  openEditDialog(customer: CustomerDto): void {
    this.dialog
      .open(CustomerEditDialogComponent, { data: { customer } })
      .afterClosed()
      .subscribe((updated: CustomerDto | undefined) => {
        if (updated) {
          this.loadCustomers(this.pagedCustomers().page);
        }
      });
  }
}
