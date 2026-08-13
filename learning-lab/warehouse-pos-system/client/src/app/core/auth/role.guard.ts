import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

// A second, role-aware layer on top of authGuard — same "UX guard, not a
// security boundary" philosophy that guard's own comment already states:
// the actual security boundary is each backend service's own
// [Authorize(Roles = ...)] (F2). Without this, a signed-in Cashier
// clicking "Admin" or "Reports" in the toolbar would land on a real page
// that immediately fails every API call with 403 — this redirects them
// before that ever renders, the identical "signed-out user sees a login
// page instead of a broken screen" reasoning applied to "signed-in but
// wrong role" instead of "not signed in at all."
export function roleGuard(allowedRoles: string[]): CanActivateFn {
  return () => {
    const authService = inject(AuthService);
    const router = inject(Router);

    const user = authService.currentUser();
    if (user && allowedRoles.includes(user.role)) {
      return true;
    }

    return router.createUrlTree(['/login']);
  };
}
