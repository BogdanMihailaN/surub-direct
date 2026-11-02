import { DestroyRef, inject, Injectable, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { AccountRecoveryApi } from '../data-access/account-recovery-api.service';
import { RecoveryLink } from '../models/recovery-link';
@Injectable()
export class AccountRecoveryStore {
  private readonly api = inject(AccountRecoveryApi);
  private generation = 0;
  readonly busy = signal(false);
  readonly error = signal('');
  readonly notice = signal('');
  readonly completed = signal(false);
  readonly link = signal<RecoveryLink | null>(null);
  readonly loaded = signal(false);
  constructor() {
    inject(DestroyRef).onDestroy(() => {
      this.generation++;
      this.link.set(null);
    });
  }
  private async run(operation: () => Promise<void>, message: string, consume = false) {
    if (this.busy() || this.completed()) return;
    const generation = ++this.generation;
    this.busy.set(true);
    this.error.set('');
    this.notice.set('');
    try {
      await operation();
      if (generation !== this.generation) return;
      this.notice.set(message);
      if (consume) {
        this.completed.set(true);
        this.link.set(null);
      }
    } catch (error) {
      if (generation !== this.generation) return;
      this.error.set(
        error instanceof HttpErrorResponse && error.status === 429
          ? 'Prea multe solicitări. Așteaptă un minut și încearcă din nou.'
          : error instanceof HttpErrorResponse && error.status === 400
            ? 'Date invalide sau link expirat ori deja folosit. Verifică parola sau solicită un mesaj nou.'
            : error instanceof HttpErrorResponse && error.status === 404
              ? 'Operațiunea nu este disponibilă în acest mediu.'
              : 'Nu putem confirma rezultatul. Cererea nu este repetată automat. Verifică accesul la cont sau solicită un mesaj nou.',
      );
    } finally {
      if (generation === this.generation) this.busy.set(false);
    }
  }
  request(kind: 'email' | 'password', email: string) {
    return this.run(
      () => this.api.request(kind, email.trim()),
      'Dacă adresa corespunde unui cont eligibil, mesajul a fost pregătit.',
    );
  }
  confirm() {
    const link = this.link();
    if (!link) return;
    return this.run(() => this.api.confirm(link), 'Adresa de e-mail a fost confirmată.', true);
  }
  reset(password: string) {
    const link = this.link();
    if (!link) return;
    return this.run(
      () => this.api.reset(link, password),
      'Parola a fost schimbată. Sesiunile anterioare au fost închise. Autentifică-te cu parola nouă.',
      true,
    );
  }
}
