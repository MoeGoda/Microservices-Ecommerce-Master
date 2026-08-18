import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
  provideZoneChangeDetection,
} from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideNativeDateAdapter } from '@angular/material/core';

import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { errorInterceptor } from './core/interceptors/error.interceptor';
import { languageInterceptor } from './core/interceptors/language.interceptor';
import { I18nService } from './core/i18n/i18n.service';

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
    provideHttpClient(withInterceptors([authInterceptor, languageInterceptor, errorInterceptor])),
    // Async variant lazy-loads the animations package instead of bundling
    // it into the initial chunk — Angular Material needs *some* animations
    // provider registered or its components (snackbar, form field) throw
    // at runtime.
    provideAnimationsAsync(),
    // M — MatDatepickerModule needs a date adapter registered somewhere;
    // provided once here rather than per-component now that the new
    // Warehouse filter panels (Receipts/Transfers/Adjustments/Issues) all
    // use a from/to date range picker.
    provideNativeDateAdapter(),
    // F3 — loads en.json/ar.json and initializes i18next before the app's
    // first render, so no component or the translate pipe ever sees an
    // un-initialized i18next instance and renders a flash of raw keys.
    provideAppInitializer(() => inject(I18nService).init()),
  ],
};
