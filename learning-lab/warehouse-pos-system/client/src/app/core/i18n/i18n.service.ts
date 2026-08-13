import { Injectable, signal } from '@angular/core';
import i18next from 'i18next';

export type SupportedLang = 'en' | 'ar';

const STORAGE_KEY = 'warehousepos.lang';
const SUPPORTED: readonly SupportedLang[] = ['en', 'ar'];

// The one place that knows i18next exists — components/pipes call
// t()/currentLang() and never touch the library directly, same "one
// gatekeeper service, everything else goes through it" shape as
// AuthService for the auth token. Resources are fetched once at app
// startup (see init(), called from an app initializer in app.config.ts)
// rather than statically imported, since the client's tsconfig doesn't
// enable resolveJsonModule and the translation files live under public/
// as plain static assets anyway.
@Injectable({ providedIn: 'root' })
export class I18nService {
  private readonly _currentLang = signal<SupportedLang>(readStoredLang());
  readonly currentLang = this._currentLang.asReadonly();

  private ready: Promise<void> | null = null;

  init(): Promise<void> {
    if (!this.ready) {
      this.ready = this.loadAndInit();
    }
    return this.ready;
  }

  private async loadAndInit(): Promise<void> {
    const lang = this._currentLang();
    const [en, ar] = await Promise.all([
      fetch('/i18n/en.json').then((r) => r.json()),
      fetch('/i18n/ar.json').then((r) => r.json()),
    ]);

    await i18next.init({
      lng: lang,
      fallbackLng: 'en',
      resources: {
        en: { translation: en },
        ar: { translation: ar },
      },
      // Angular templates already escape interpolated values — no need
      // for i18next's own HTML-escaping on top of that.
      interpolation: { escapeValue: false },
    });

    applyDocumentDirection(lang);
  }

  // Any string not found in the active language's resource file falls
  // back to fallbackLng ('en') automatically; a key missing from both
  // resource files renders as the raw key itself, same as i18next's own
  // default behavior — visibly wrong rather than silently blank.
  t(key: string, options?: Record<string, unknown>): string {
    return i18next.t(key, options);
  }

  isRtl(): boolean {
    return this._currentLang() === 'ar';
  }

  // A full reload rather than a live re-render: Angular Material's CDK
  // Directionality reads the `dir` attribute once, at each component's
  // construction — flipping it at runtime wouldn't re-flow already-built
  // Material components (mat-form-field, mat-menu, ...) to RTL. A reload
  // is a deliberate, documented scope cut for this phase (see the
  // README's F3 gap note) rather than half-working live RTL.
  switchLanguage(lang: SupportedLang): void {
    if (lang === this._currentLang()) {
      return;
    }

    localStorage.setItem(STORAGE_KEY, lang);
    applyDocumentDirection(lang);
    location.reload();
  }
}

function readStoredLang(): SupportedLang {
  const stored = localStorage.getItem(STORAGE_KEY);
  return isSupportedLang(stored) ? stored : 'en';
}

function isSupportedLang(value: string | null): value is SupportedLang {
  return !!value && SUPPORTED.includes(value as SupportedLang);
}

// Exported so main.ts can call it synchronously before bootstrap — setting
// dir/lang on <html> before Angular ever renders avoids a flash of
// LTR-then-RTL content on an Arabic-preferring reload.
export function applyDocumentDirection(lang: SupportedLang): void {
  document.documentElement.lang = lang;
  document.documentElement.dir = lang === 'ar' ? 'rtl' : 'ltr';
}

export function currentStoredLang(): SupportedLang {
  return readStoredLang();
}
