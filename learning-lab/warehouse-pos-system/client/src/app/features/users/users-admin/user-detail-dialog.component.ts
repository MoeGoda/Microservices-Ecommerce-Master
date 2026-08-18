import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { finalize } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';
import { I18nService } from '../../../core/i18n/i18n.service';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { NotificationService } from '../../../core/notifications/notification.service';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { UserDto } from '../../../shared/models/users.models';
import { UsersService } from '../users.service';

export interface UserDetailDialogData {
  user: UserDto;
}

// L — the "click a row's action button, see everything about it in a
// popup" pattern requested for this screen. isSelf still guards against
// the signed-in Admin deactivating their own row — the backend's own
// SetUserActiveCommandHandler is the real enforcement (see
// UsersAdminComponent's original comment); this is only the same UX
// nicety, now living here instead.
@Component({
  selector: 'app-user-detail-dialog',
  imports: [DatePipe, MatButtonModule, MatDialogModule, StatusBadgeComponent, TranslatePipe],
  templateUrl: './user-detail-dialog.component.html',
  styleUrl: './user-detail-dialog.component.scss',
})
export class UserDetailDialogComponent {
  private readonly data = inject<UserDetailDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject<MatDialogRef<UserDetailDialogComponent, UserDto>>(MatDialogRef);
  private readonly usersService = inject(UsersService);
  private readonly notification = inject(NotificationService);
  private readonly authService = inject(AuthService);
  private readonly i18n = inject(I18nService);

  readonly user = signal<UserDto>(this.data.user);
  readonly togglingActive = signal(false);

  isSelf(): boolean {
    return this.user().userName === this.authService.currentUser()?.userName;
  }

  toggleActive(): void {
    if (this.togglingActive() || (this.user().isActive && this.isSelf())) {
      return;
    }

    const nextActive = !this.user().isActive;
    this.togglingActive.set(true);
    this.usersService
      .setActive(this.user().id, nextActive)
      .pipe(finalize(() => this.togglingActive.set(false)))
      .subscribe({
        next: (updated) => {
          this.notification.success(this.i18n.t(nextActive ? 'users.toasts.activated' : 'users.toasts.deactivated', { userName: updated.userName }));
          this.user.set(updated);
          this.dialogRef.close(updated);
        },
      });
  }
}
