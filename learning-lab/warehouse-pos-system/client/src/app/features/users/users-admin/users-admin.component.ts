import { DatePipe } from '@angular/common';
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
import { UserDto } from '../../../shared/models/users.models';
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
@Component({
  selector: 'app-users-admin',
  imports: [DatePipe, MatButtonModule, MatCardModule, MatDialogModule, MatIconModule, MatPaginatorModule, MatProgressSpinnerModule, TranslatePipe],
  templateUrl: './users-admin.component.html',
  styleUrl: './users-admin.component.scss',
})
export class UsersAdminComponent implements OnInit {
  readonly pagedUsers = signal<PagedResult<UserDto>>(emptyPage());
  readonly loadingUsers = signal(false);

  constructor(
    private readonly usersService: UsersService,
    private readonly dialog: MatDialog,
  ) {}

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(page = 1): void {
    this.loadingUsers.set(true);
    this.usersService
      .getUsers(page, this.pagedUsers().pageSize)
      .pipe(finalize(() => this.loadingUsers.set(false)))
      .subscribe({ next: (result) => this.pagedUsers.set(result) });
  }

  onUsersPageChange(event: PageEvent): void {
    if (event.pageSize !== this.pagedUsers().pageSize) {
      this.pagedUsers.update((current) => ({ ...current, pageSize: event.pageSize }));
    }
    this.loadUsers(event.pageIndex + 1);
  }

  openCreateDialog(): void {
    this.dialog
      .open(UserCreateDialogComponent)
      .afterClosed()
      .subscribe((created) => {
        if (created) {
          this.loadUsers(this.pagedUsers().page);
        }
      });
  }

  openDetailDialog(user: UserDto): void {
    this.dialog
      .open(UserDetailDialogComponent, { data: { user } })
      .afterClosed()
      .subscribe((updated) => {
        if (updated) {
          this.pagedUsers.update((current) => ({
            ...current,
            items: current.items.map((u) => (u.id === updated.id ? updated : u)),
          }));
        }
      });
  }
}
