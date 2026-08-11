import { Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

// One place that knows how to show a toast — components and the HTTP error
// interceptor both call this instead of each rolling their own MatSnackBar
// config. Success and error toasts get distinct CSS classes (see
// styles.scss) so they're visually distinguishable at a glance, matching
// the "Toaster Success and Error Messages" requirement directly.
@Injectable({ providedIn: 'root' })
export class NotificationService {
  constructor(private readonly snackBar: MatSnackBar) {}

  success(message: string): void {
    this.snackBar.open(message, 'Dismiss', {
      duration: 4000,
      panelClass: ['toast-success'],
      horizontalPosition: 'right',
      verticalPosition: 'top',
    });
  }

  error(message: string): void {
    this.snackBar.open(message, 'Dismiss', {
      duration: 6000,
      panelClass: ['toast-error'],
      horizontalPosition: 'right',
      verticalPosition: 'top',
    });
  }
}
