import {
  ApplicationConfig,
  provideBrowserGlobalErrorListeners,
  provideZoneChangeDetection,
} from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { errorInterceptor } from './core/interceptors/error.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    // Order matters: authInterceptor runs first and attaches the token,
    // THEN the request goes out and errorInterceptor only sees the
    // response side — but interceptors execute in array order for the
    // request phase and reverse order for the response phase, so
    // errorInterceptor's catchError still sees every request's outcome
    // regardless of this ordering.
    provideHttpClient(withInterceptors([authInterceptor, errorInterceptor])),
    // Async variant lazy-loads the animations package instead of bundling
    // it into the initial chunk — Angular Material needs *some* animations
    // provider registered or its components (snackbar, form field) throw
    // at runtime.
    provideAnimationsAsync(),
  ],
};
