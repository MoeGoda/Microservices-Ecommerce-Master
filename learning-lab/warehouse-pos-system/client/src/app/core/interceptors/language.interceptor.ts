import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { I18nService } from '../i18n/i18n.service';

// The other half of F3's backend culture negotiation (Common.RequestCulture):
// every outgoing API call carries the same language the UI is currently
// showing, so a Cashier who switched to Arabic sees Arabic validation
// errors from the backend too, not just Arabic static UI text.
export const languageInterceptor: HttpInterceptorFn = (req, next) => {
  const i18n = inject(I18nService);

  return next(
    req.clone({
      setHeaders: { 'Accept-Language': i18n.currentLang() },
    }),
  );
};
