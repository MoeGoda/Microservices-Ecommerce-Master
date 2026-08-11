import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../auth/auth.service';

// Functional interceptor (the modern Angular 15+ style — no NgModule, no
// class implementing HttpInterceptor, just a function registered in
// app.config.ts). Attaches the stored JWT to every outgoing request; the
// backend decides per-route whether it actually required one — this
// interceptor doesn't know or care which routes are protected.
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const token = authService.getToken();

  if (!token) {
    return next(req);
  }

  return next(
    req.clone({
      setHeaders: { Authorization: `Bearer ${token}` },
    }),
  );
};
