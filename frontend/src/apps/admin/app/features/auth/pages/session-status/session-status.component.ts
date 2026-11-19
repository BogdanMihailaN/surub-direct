import { Location } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { SessionService } from '../../../../core/auth/session.service';

@Component({
  selector: 'sd-admin-session-status',
  templateUrl: './session-status.component.html',
  styleUrl: './session-status.component.scss',
})
export class SessionStatusComponent {
  private readonly router = inject(Router);
  private readonly session = inject(SessionService);
  private readonly state = (this.router.currentNavigation()?.extras.state ??
    inject(Location).getState()) as Record<string, unknown> | undefined;
  readonly forbidden = this.state?.['status'] === 'forbidden';
  readonly busy = signal(false);
  readonly error = signal('');

  async retry() {
    if (this.busy()) return;
    const returnTo = this.state?.['returnTo'];
    const path = typeof returnTo === 'string' && /^\/(?!\/)/.test(returnTo) ? returnTo : '/catalog';
    this.busy.set(true);
    try {
      await this.router.navigateByUrl(path, { replaceUrl: true });
    } finally {
      this.busy.set(false);
    }
  }

  async changeAccount() {
    this.busy.set(true);
    this.error.set('');
    try {
      await this.session.logout();
      await this.router.navigate(['/login'], { replaceUrl: true });
    } catch {
      this.error.set('Deconectarea nu a putut fi confirmată. Reîncearcă.');
    } finally {
      this.busy.set(false);
    }
  }
}
