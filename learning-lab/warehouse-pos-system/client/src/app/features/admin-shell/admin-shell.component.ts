import { Component } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';
import { ItemsAdminComponent } from '../warehouse/items-admin/items-admin.component';
import { TranslatePipe } from '../../core/i18n/translate.pipe';

// Was a placeholder through A4 — proving the auth chain (login → token
// stored → guard lets you in → currentUser signal populated) worked end to
// end. Now it hosts the real B3 content: still shows who's signed in, and
// still sits behind authGuard, but the "lands here in Step B3" note it used
// to carry is exactly what ItemsAdminComponent below now is.
@Component({
  selector: 'app-admin-shell',
  imports: [ItemsAdminComponent, TranslatePipe],
  templateUrl: './admin-shell.component.html',
  styleUrl: './admin-shell.component.scss',
})
export class AdminShellComponent {
  constructor(readonly authService: AuthService) {}
}
