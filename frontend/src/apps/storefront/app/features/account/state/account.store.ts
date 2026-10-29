import { inject, Injectable, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { errorMessage } from '@shared/http/error-message';
import { AccountApi } from '../data-access/account-api.service';
import { AccountSession } from '../models/account-session';
import { CartStore } from '../../cart/state/cart.store';
@Injectable()
export class AccountStore {
  private readonly api = inject(AccountApi);
  private readonly cart = inject(CartStore);
  readonly session = signal<AccountSession | null>(null);
  readonly status = signal<'loading' | 'guest' | 'authenticated' | 'unavailable'>('loading');
  readonly busy = signal(false);
  readonly error = signal('');
  readonly notice = signal('');
  async load() {
    if (this.busy()) return;
    this.busy.set(true);
    this.error.set('');
    this.notice.set('');
    this.session.set(null);
    this.status.set('loading');
    try {
      this.session.set(await this.api.session());
      this.status.set('authenticated');
    } catch (e) {
      if (e instanceof HttpErrorResponse && e.status === 401) this.status.set('guest');
      else {
        this.status.set('unavailable');
        this.error.set(errorMessage(e));
      }
    } finally {
      this.busy.set(false);
    }
  }
  async submit(email: string, password: string, register: boolean) {
    if (this.busy() || this.status() !== 'guest') return;
    this.busy.set(true);
    this.error.set('');
    this.notice.set('');
    let signedIn = false;
    try {
      if (register) {
        await this.api.register(email.trim(), password);
        this.notice.set(
          'Cererea a fost procesată. Te poți autentifica folosind parola contului tău. Un cont existent își păstrează parola.',
        );
      } else {
        await this.api.login(email.trim(), password);
        signedIn = true;
        this.session.set(await this.api.session());
        this.status.set('authenticated');
      }
    } catch (e) {
      if (
        signedIn ||
        (e instanceof HttpErrorResponse &&
          (e.status === 409 || (!register && (e.status === 0 || e.status >= 500))))
      )
        this.status.set('unavailable');
      this.error.set(
        e instanceof HttpErrorResponse && e.status === 429
          ? 'Prea multe încercări. Așteaptă un minut și încearcă din nou.'
          : register && e instanceof HttpErrorResponse && e.status === 404
            ? 'Înregistrarea este disponibilă momentan doar în mediul demonstrativ de dezvoltare.'
            : errorMessage(e),
      );
    } finally {
      if (!register) this.cart.revalidate();
      this.busy.set(false);
    }
  }
  async logout() {
    if (this.busy()) return;
    this.busy.set(true);
    this.error.set('');
    this.notice.set('');
    try {
      await this.api.logout();
      this.status.set('guest');
    } catch (e) {
      if (e instanceof HttpErrorResponse && e.status === 401) this.status.set('guest');
      else {
        this.status.set('unavailable');
        this.error.set(errorMessage(e));
      }
    } finally {
      this.cart.revalidate();
      this.session.set(null);
      this.busy.set(false);
    }
  }
}
