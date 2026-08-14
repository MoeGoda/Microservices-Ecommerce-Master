import { Pipe, PipeTransform } from '@angular/core';
import { I18nService } from './i18n.service';

// Impure by design (pure: false): a plain pure pipe only re-evaluates when
// its own arguments change, but a language switch changes what the SAME
// key/args should render as. I18nService.switchLanguage() reloads the page
// anyway (see its own comment on why), so this never actually needs to
// react to a live language change mid-session — it stays impure only so a
// key can be looked up freshly on every change-detection pass without
// requiring every call site to pass currentLang() in as a fake extra arg.
@Pipe({ name: 'translate', standalone: true, pure: false })
export class TranslatePipe implements PipeTransform {
  constructor(private readonly i18n: I18nService) {}

  transform(key: string, options?: Record<string, unknown>): string {
    return this.i18n.t(key, options);
  }
}
