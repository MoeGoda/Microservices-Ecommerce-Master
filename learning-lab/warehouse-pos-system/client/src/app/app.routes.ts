import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { LoginComponent } from './features/login/login.component';
import { AdminShellComponent } from './features/admin-shell/admin-shell.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'login' },
  { path: 'login', component: LoginComponent },
  { path: 'admin', component: AdminShellComponent, canActivate: [authGuard] },
  { path: '**', redirectTo: 'login' },
];
