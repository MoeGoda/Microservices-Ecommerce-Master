import { Component, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-login',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  // Reactive Forms, not template-driven: validation rules live here as
  // code (Validators.required etc.), the same "reject bad input before it
  // does anything" idea as the backend's FluentValidation — just running
  // client-side, before a request is even sent, as a fast first pass.
  // The backend re-validates everything regardless; this form existing
  // doesn't change that (a fetch()/curl call bypasses this entirely).
  readonly form = new FormGroup({
    userName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    password: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  readonly loading = signal(false);

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router,
  ) {}

  submit(): void {
    if (this.form.invalid || this.loading()) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    const { userName, password } = this.form.getRawValue();

    this.authService
      .login({ userName, password })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        // Errors are already turned into a toast by errorInterceptor — this
        // component only needs to react to success. There's deliberately
        // no .subscribe({ error: ... }) here duplicating that.
        next: () => this.router.navigateByUrl('/admin'),
      });
  }
}
