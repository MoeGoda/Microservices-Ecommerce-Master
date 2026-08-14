import { DatePipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { finalize } from 'rxjs';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { AuthService } from '../../core/auth/auth.service';
import { NotificationService } from '../../core/notifications/notification.service';
import { emptyPage, PagedResult } from '../../shared/models/pagination.models';
import { ROLES } from '../../shared/models/roles';
import { UserDto } from '../../shared/models/users.models';
import { UsersService } from './users.service';

// H — the Admin-only screen for managing other accounts. Deliberately
// separate from AdminShellComponent/ItemsAdminComponent: this manages
// people, not warehouse data, and has its own [Authorize(Roles=Admin)]
// backend restriction (no Manager/staff carve-out) rather than the
// broader ADMIN_ROLES set the warehouse screen uses.
@Component({
  selector: 'app-users-admin',
  imports: [
    DatePipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    TranslatePipe,
  ],
  templateUrl: './users-admin.component.html',
  styleUrl: './users-admin.component.scss',
})
export class UsersAdminComponent implements OnInit {
  readonly roles = Object.values(ROLES);

  readonly pagedUsers = signal<PagedResult<UserDto>>(emptyPage());
  readonly loadingUsers = signal(false);
  readonly creatingUser = signal(false);
  // The one row currently being activated/deactivated, if any — disables
  // just that row's toggle rather than every row's while the request is
  // in flight, same reasoning as items-admin's cancellingPromotionId.
  readonly togglingUserId = signal<number | null>(null);

  readonly createForm = new FormGroup({
    userName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
    password: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(8)] }),
    firstName: new FormControl('', { nonNullable: true }),
    lastName: new FormControl('', { nonNullable: true }),
    role: new FormControl<string>(ROLES.Cashier, { nonNullable: true, validators: [Validators.required] }),
  });

  constructor(
    private readonly usersService: UsersService,
    private readonly notification: NotificationService,
    private readonly authService: AuthService,
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

  submitCreate(): void {
    if (this.createForm.invalid || this.creatingUser()) {
      this.createForm.markAllAsTouched();
      return;
    }

    const value = this.createForm.getRawValue();
    this.creatingUser.set(true);
    this.usersService
      .createUser(value)
      .pipe(finalize(() => this.creatingUser.set(false)))
      .subscribe({
        // A 4xx (duplicate username/email, weak password) is already
        // turned into a toast by errorInterceptor — this only needs the
        // success path.
        next: () => {
          this.notification.success(`User "${value.userName}" created.`);
          this.createForm.reset({ userName: '', email: '', password: '', firstName: '', lastName: '', role: ROLES.Cashier });
          this.loadUsers();
        },
      });
  }

  // The backend's own self-deactivation guard (SetUserActiveCommandHandler)
  // is what actually protects this — this is purely a UX nicety so the
  // toggle for the signed-in Admin's own row doesn't even look clickable.
  isSelf(user: UserDto): boolean {
    return user.userName === this.authService.currentUser()?.userName;
  }

  toggleActive(user: UserDto): void {
    if (this.togglingUserId() || (user.isActive && this.isSelf(user))) {
      return;
    }

    const nextActive = !user.isActive;
    this.togglingUserId.set(user.id);
    this.usersService
      .setActive(user.id, nextActive)
      .pipe(finalize(() => this.togglingUserId.set(null)))
      .subscribe({
        next: (updated) => {
          this.notification.success(nextActive ? `${updated.userName} activated.` : `${updated.userName} deactivated.`);
          this.pagedUsers.update((current) => ({
            ...current,
            items: current.items.map((u) => (u.id === updated.id ? updated : u)),
          }));
        },
      });
  }
}
