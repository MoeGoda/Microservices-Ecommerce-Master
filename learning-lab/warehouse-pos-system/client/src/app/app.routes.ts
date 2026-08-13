import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { LoginComponent } from './features/login/login.component';
import { AdminShellComponent } from './features/admin-shell/admin-shell.component';
import { PosRegisterComponent } from './features/pos/pos-register.component';
import { ReportsDashboardComponent } from './features/reporting/reports-dashboard/reports-dashboard.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'login' },
  { path: 'login', component: LoginComponent },
  { path: 'admin', component: AdminShellComponent, canActivate: [authGuard] },
  // No AdminShellComponent-style wrapper here — that shell only exists
  // because it was A4's placeholder before B3's real content landed in it;
  // there's no equivalent precedent for POS or Reports, so these routes
  // point straight at their feature components.
  { path: 'pos', component: PosRegisterComponent, canActivate: [authGuard] },
  { path: 'reports', component: ReportsDashboardComponent, canActivate: [authGuard] },
  { path: '**', redirectTo: 'login' },
];
