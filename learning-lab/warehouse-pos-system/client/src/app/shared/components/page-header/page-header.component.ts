import { Component, Input } from '@angular/core';

// M — every list/detail screen repeated the same `.page-header` markup
// (title + an actions slot) by hand. This wraps that markup once;
// `title`/`subtitle` take already-translated strings (the caller still
// owns its own `| translate` calls) so this component stays i18n-agnostic,
// the same design choice NotificationService already makes for its
// message strings.
@Component({
  selector: 'app-page-header',
  templateUrl: './page-header.component.html',
  styleUrl: './page-header.component.scss',
})
export class PageHeaderComponent {
  @Input({ required: true }) title!: string;
  @Input() subtitle?: string;
}
