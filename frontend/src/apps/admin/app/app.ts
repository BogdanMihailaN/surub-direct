import { Component, inject, signal } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { SessionService } from './core/auth/session.service';
import { errorMessage } from '@shared/http/error-message';
@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, MatButtonModule],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  readonly session = inject(SessionService);
  private readonly router = inject(Router);
  readonly error = signal('');
  async logout() {
    try {
      await this.session.logout();
      await this.router.navigate(['/login']);
    } catch (e) {
      this.error.set(errorMessage(e));
    }
  }
}
