import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { roleGuard } from './core/auth/role.guard';
import { LoginComponent } from './features/login/login.component';
import { AdminShellComponent } from './features/admin-shell/admin-shell.component';
import { PosRegisterComponent } from './features/pos/pos-register.component';
import { ReportsDashboardComponent } from './features/reporting/reports-dashboard/reports-dashboard.component';
import { UsersAdminComponent } from './features/users/users-admin.component';
import { ADMIN_ROLES, POS_ROLES, REPORTS_ROLES, USER_MANAGEMENT_ROLES } from './shared/models/roles';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'login' },
  { path: 'login', component: LoginComponent },
  // F2 — authGuard alone only checked "is anyone signed in"; roleGuard
  // adds "is THIS the role this area is for," matching each area's own
  // backend [Authorize(Roles = ...)] restriction one-for-one.
  { path: 'admin', component: AdminShellComponent, canActivate: [authGuard, roleGuard(ADMIN_ROLES)] },
  // No AdminShellComponent-style wrapper here — that shell only exists
  // because it was A4's placeholder before B3's real content landed in it;
  // there's no equivalent precedent for POS or Reports, so these routes
  // point straight at their feature components.
  { path: 'pos', component: PosRegisterComponent, canActivate: [authGuard, roleGuard(POS_ROLES)] },
  { path: 'reports', component: ReportsDashboardComponent, canActivate: [authGuard, roleGuard(REPORTS_ROLES)] },
  { path: 'users', component: UsersAdminComponent, canActivate: [authGuard, roleGuard(USER_MANAGEMENT_ROLES)] },
  { path: '**', redirectTo: 'login' },
];
