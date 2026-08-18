import { DatePipe } from '@angular/common';
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
import { ROLES } from '../../../shared/models/roles';
import { UserDto } from '../../../shared/models/users.models';
import { paginateClientSide } from '../../../shared/utils/paginate-client-side';
import { UsersService } from '../users.service';
import { UserCreateDialogComponent } from './user-create-dialog.component';
import { UserDetailDialogComponent } from './user-detail-dialog.component';

// L — the Admin-only screen for managing other accounts. The create
// form and the per-row detail/activate-deactivate panel both moved into
// dialogs (UserCreateDialogComponent / UserDetailDialogComponent), so
// this component only owns the paged list itself. Deliberately separate
// from Warehouse's own screens: this manages people, not warehouse
// data, and has its own [Authorize(Roles=Admin)] backend restriction
// (no Manager/staff carve-out) rather than the broader ADMIN_ROLES set
// the warehouse screens use.
// M — GetUsersQuery has no server-side search (only page/pageSize), so
// the new filter panel searches/filters client-side over the fetched
// batch, same paginateClientSide pattern used elsewhere.
@Component({
  selector: 'app-users-admin',
  imports: [
    DatePipe,
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
  templateUrl: './users-admin.component.html',
  styleUrl: './users-admin.component.scss',
})
export class UsersAdminComponent implements OnInit {
  readonly pagedUsers = signal<PagedResult<UserDto>>(emptyPage());
  readonly loadingUsers = signal(false);
  readonly roles = Object.values(ROLES);

  private allUsers: UserDto[] = [];

  readonly filterForm = new FormGroup({
    search: new FormControl('', { nonNullable: true }),
    role: new FormControl<string | null>(null),
    status: new FormControl<'active' | 'inactive' | null>(null),
  });

  constructor(
    private readonly usersService: UsersService,
    private readonly dialog: MatDialog,
  ) {}

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.loadingUsers.set(true);
    this.usersService
      .getUsers(1, 100)
      .pipe(finalize(() => this.loadingUsers.set(false)))
      .subscribe({
        next: (result) => {
          this.allUsers = result.items;
          this.applyClientFilters(1);
        },
      });
  }

  applyClientFilters(page: number): void {
    const value = this.filterForm.getRawValue();
    const search = value.search.trim().toLowerCase();
    const filtered = this.allUsers.filter(
      (u) =>
        (!search ||
          u.userName.toLowerCase().includes(search) ||
          u.email.toLowerCase().includes(search) ||
          `${u.firstName} ${u.lastName}`.toLowerCase().includes(search)) &&
        (!value.role || u.role === value.role) &&
        (!value.status || (value.status === 'active' ? u.isActive : !u.isActive)),
    );
    this.pagedUsers.set(paginateClientSide(filtered, page, this.pagedUsers().pageSize));
  }

  onSearch(): void {
    this.applyClientFilters(1);
  }

  onResetFilters(): void {
    this.filterForm.reset({ search: '', role: null, status: null });
    this.applyClientFilters(1);
  }

  onUsersPageChange(event: PageEvent): void {
    if (event.pageSize !== this.pagedUsers().pageSize) {
      this.pagedUsers.update((current) => ({ ...current, pageSize: event.pageSize }));
    }
    this.applyClientFilters(event.pageIndex + 1);
  }

  openCreateDialog(): void {
    this.dialog
      .open(UserCreateDialogComponent)
      .afterClosed()
      .subscribe((created) => {
        if (created) {
          this.loadUsers();
        }
      });
  }

  openDetailDialog(user: UserDto): void {
    this.dialog
      .open(UserDetailDialogComponent, { data: { user } })
      .afterClosed()
      .subscribe((updated) => {
        if (updated) {
          this.allUsers = this.allUsers.map((u) => (u.id === updated.id ? updated : u));
          this.applyClientFilters(this.pagedUsers().page);
        }
      });
  }
}
