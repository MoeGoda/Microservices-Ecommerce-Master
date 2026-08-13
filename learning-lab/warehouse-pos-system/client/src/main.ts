import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';
import { applyDocumentDirection, currentStoredLang } from './app/core/i18n/i18n.service';

// Set dir/lang on <html> before Angular renders anything — avoids a flash
// of LTR content on a reload where the user had already switched to
// Arabic. The persisted language is re-read (not injected) inside
// I18nService itself once DI is up; this is just the pre-bootstrap sync
// half of the same "read from localStorage at startup" pattern
// AuthService already uses for the auth token.
applyDocumentDirection(currentStoredLang());

bootstrapApplication(App, appConfig)
  .catch((err) => console.error(err));
