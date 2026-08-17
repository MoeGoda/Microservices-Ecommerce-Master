import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { I18nService } from '../i18n/i18n.service';
import { NotificationService } from '../notifications/notification.service';
import { ProblemDetails } from '../../shared/models/problem-details.model';

// The single place that turns an HTTP error into a toast. Individual
// components don't each write their own .subscribe({ error: ... }) toast
// logic — they can, for something route-specific, but the baseline "show
// the user what went wrong" behaviour is handled once, here, for every
// request in the app. This is the frontend mirror of A2's
// GlobalExceptionHandler: that gave every backend error one consistent
// shape (ProblemDetails); this is the one place that reads that shape.
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const notification = inject(NotificationService);
  const authService = inject(AuthService);
  const router = inject(Router);
  const i18n = inject(I18nService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      const isLoginRequest = req.url.endsWith('/Identity/Auth/login');

      if (error.status === 401 && !isLoginRequest) {
        // A 401 on anything *other* than the login call itself means the
        // token we sent was rejected — expired, or the server's signing key
        // changed. There's no scenario where retrying without signing in
        // again helps, so force it.
        authService.logout();
        notification.error(i18n.t('common.errors.sessionExpired'));
        router.navigateByUrl('/login');
      } else if (error.status === 429) {
        // The gateway's RateLimiter (A3) rejects with a bare 429 and no
        // body — there's no ProblemDetails to read here, unlike every
        // other error path in this app.
        notification.error(i18n.t('common.errors.tooManyAttempts'));
      } else {
        notification.error(describeError(error, i18n));
      }

      return throwError(() => error);
    }),
  );
};

function describeError(error: HttpErrorResponse, i18n: I18nService): string {
  const problem = error.error as ProblemDetails | undefined;

  if (problem?.errors) {
    // Field-level validation errors (Common.Exceptions.ValidationException,
    // A2) — flatten "Password: must contain a digit" style messages into
    // one toast rather than showing only the first field's error. The
    // messages themselves are already in the right language — the backend's
    // own culture negotiation (F3) reads the same Accept-Language header
    // languageInterceptor attaches from I18nService.currentLang().
    return Object.entries(problem.errors)
      .flatMap(([field, messages]) => messages.map((m) => `${field}: ${m}`))
      .join(' ');
  }

  return problem?.detail || i18n.t('common.errors.somethingWentWrong');
}
