import { Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { I18nService, SupportedLang } from '../../../core/i18n/i18n.service';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';

// G — pulled out of app.ts so the same switcher can render in both the
// authenticated shell's topbar and the bare (logged-out) login topbar
// without duplicating the button+menu markup in app.html twice.
@Component({
  selector: 'app-language-switcher',
  imports: [MatButtonModule, MatMenuModule, TranslatePipe],
  template: `
    <button
      mat-button
      [matMenuTriggerFor]="languageMenu"
      [attr.aria-label]="'toolbar.language' | translate"
      [attr.title]="'toolbar.language' | translate"
    >
      {{ i18n.currentLang() === 'ar' ? 'AR' : 'EN' }}
    </button>
    <mat-menu #languageMenu="matMenu">
      <button mat-menu-item (click)="switchLanguage('en')">English</button>
      <button mat-menu-item (click)="switchLanguage('ar')">العربية</button>
    </mat-menu>
  `,
})
export class LanguageSwitcherComponent {
  constructor(readonly i18n: I18nService) {}

  switchLanguage(lang: SupportedLang): void {
    this.i18n.switchLanguage(lang);
  }
}
