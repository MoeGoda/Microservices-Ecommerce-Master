import { Component, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { finalize } from 'rxjs';
import { I18nService } from '../../../core/i18n/i18n.service';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { NotificationService } from '../../../core/notifications/notification.service';
import { ROLES } from '../../../shared/models/roles';
import { UsersService } from '../users.service';

// L — the create half of what used to be an inline card above the
// Users grid. AuthController.CreateUser (the F2 endpoint this calls)
// doesn't return the created UserDto, so this closes with a plain
// `true` rather than the row itself — the grid reloads on any truthy
// close, same as it always has.
@Component({
  selector: 'app-user-create-dialog',
  imports: [ReactiveFormsModule, MatButtonModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatProgressSpinnerModule, MatSelectModule, TranslatePipe],
  templateUrl: './user-create-dialog.component.html',
  styleUrl: './user-create-dialog.component.scss',
})
export class UserCreateDialogComponent {
  readonly roles = Object.values(ROLES);
  readonly creatingUser = signal(false);

  readonly createForm = new FormGroup({
    userName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
    password: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(8)] }),
    firstName: new FormControl('', { nonNullable: true }),
    lastName: new FormControl('', { nonNullable: true }),
    role: new FormControl<string>(ROLES.Cashier, { nonNullable: true, validators: [Validators.required] }),
  });

  constructor(
    private readonly dialogRef: MatDialogRef<UserCreateDialogComponent, boolean>,
    private readonly usersService: UsersService,
    private readonly notification: NotificationService,
    private readonly i18n: I18nService,
  ) {}

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
          this.notification.success(this.i18n.t('users.toasts.created', { userName: value.userName }));
          this.dialogRef.close(true);
        },
      });
  }
}
