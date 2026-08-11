import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

// Functional route guard (Angular 15+ style — a plain function, not a class
// implementing CanActivate). Blocks navigation into the admin shell (and,
// later, every other protected area) for anyone without a live session.
//
// This is a UX guard, not a security boundary: a determined user could
// still hand-craft a request straight to a protected API without ever
// loading this route. The actual security boundary is the gateway's JWT
// validation (A3) and each service's own [Authorize] — this guard exists
// so a signed-out user sees a login page instead of a broken, half-loaded
// admin screen full of failed API calls.
export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree(['/login']);
};
