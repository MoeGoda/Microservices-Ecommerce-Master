import { Component } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';

// Deliberately just a placeholder — the actual Admin Panel content (master
// data management) is B3's job. What matters here is that it exists behind
// authGuard and can read the signed-in user, proving A4's whole auth chain
// (login → token stored → guard lets you in → currentUser signal populated)
// actually works end to end.
@Component({
  selector: 'app-admin-shell',
  template: `
    <div class="placeholder">
      <h2>Admin area</h2>
      @if (authService.currentUser(); as user) {
        <p>Signed in as <strong>{{ user.userName }}</strong> ({{ user.role }}).</p>
      }
      <p class="note">
        This is a placeholder — Warehouse master data management lands here in Step B3.
      </p>
    </div>
  `,
  styles: `
    .placeholder {
      padding: 32px;
    }
    .note {
      color: #666;
      font-style: italic;
    }
  `,
})
export class AdminShellComponent {
  constructor(readonly authService: AuthService) {}
}
