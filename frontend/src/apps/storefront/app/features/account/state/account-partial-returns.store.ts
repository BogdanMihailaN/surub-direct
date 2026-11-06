import { DestroyRef, inject, Injectable, signal } from '@angular/core';
import { errorMessage } from '@shared/http/error-message';
import { AccountPartialReturnsApi } from '../data-access/account-partial-returns-api.service';
import { AccountPartialReturns } from '../models/account-partial-returns.model';
import { AccountPartialSelection } from '../models/account-partial-selection.model';
@Injectable()
export class AccountPartialReturnsStore {
  private readonly api = inject(AccountPartialReturnsApi);
  private sequence = 0;
  readonly data = signal<AccountPartialReturns | null>(null);
  readonly busy = signal(false);
  readonly error = signal('');
  readonly notice = signal('');
  constructor() { inject(DestroyRef).onDestroy(() => { this.sequence++; this.data.set(null); }); }
  async load(orderId: string, accountId: string) {
    if (this.busy()) return;
    const sequence = ++this.sequence;
    this.data.set(null); this.busy.set(true); this.error.set(''); this.notice.set('');
    try { const value = await this.api.read(orderId, accountId); if (sequence === this.sequence) this.data.set(value); }
    catch (error) { if (sequence === this.sequence) this.error.set(errorMessage(error)); }
    finally { if (sequence === this.sequence) this.busy.set(false); }
  }
  async request(orderId: string, accountId: string, selection: AccountPartialSelection) {
    if (this.busy() || !this.data()?.canRequest) return;
    const sequence = ++this.sequence;
    const requestId = crypto.randomUUID();
    this.busy.set(true); this.data.set(null); this.error.set(''); this.notice.set('');
    try {
      try { await this.api.request(orderId, accountId, requestId, selection); }
      catch (error) { if (sequence === this.sequence) this.error.set(errorMessage(error) + ' Starea este recitită; cererea nu este retrimisă automat.'); }
      if (sequence !== this.sequence) return;
      try {
        const value = await this.api.read(orderId, accountId);
        if (sequence !== this.sequence) return;
        this.data.set(value);
        if (value.items.some(x => x.id === requestId)) { this.error.set(''); this.notice.set('Cererea parțială este înregistrată. Consultă starea mai jos.'); }
      } catch (error) { if (sequence === this.sequence) this.error.set(errorMessage(error) + ' Actualizează retururile înainte de o nouă cerere.'); }
    } finally { if (sequence === this.sequence) this.busy.set(false); }
  }
}
