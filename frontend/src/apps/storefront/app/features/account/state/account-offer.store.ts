import { afterNextRender, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { errorMessage } from '@shared/http/error-message';
import { AccountApi } from '../data-access/account-api.service';
import { AccountCartApi } from '../data-access/account-cart-api.service';
import { ACCOUNT_CART_CHANGE_KEY } from '../data-access/account-cart-change-key';
import { AccountOfferSelection } from '../models/account-offer-selection';
import { CartSelection } from '../../cart/models/cart-selection.model';
@Injectable()
export class AccountOfferStore {
  private readonly identity = inject(AccountApi);
  private readonly api = inject(AccountCartApi);
  private generation = 0;
  readonly selected = signal<AccountOfferSelection | null>(null);
  readonly busy = signal(false);
  readonly error = signal('');
  readonly notice = signal('');
  constructor() {
    const destroy = inject(DestroyRef);
    destroy.onDestroy(() => this.invalidate());
    afterNextRender(() => {
      const invalidate = () => this.invalidate();
      const storage = (event: StorageEvent) => {
        if (event.key === ACCOUNT_CART_CHANGE_KEY || event.key === null) this.invalidate();
      };
      window.addEventListener('focus', invalidate);
      window.addEventListener('storage', storage);
      destroy.onDestroy(() => {
        window.removeEventListener('focus', invalidate);
        window.removeEventListener('storage', storage);
      });
    });
  }
  invalidate() {
    this.generation++;
    this.selected.set(null);
    this.notice.set('');
  }
  async select(line: CartSelection) {
    if (this.busy()) return;
    this.invalidate();
    const generation = this.generation;
    this.busy.set(true);
    this.error.set('');
    try {
      const session = await this.identity.session();
      const cart = await this.api.read(session.id);
      if (generation !== this.generation) return;
      if (cart?.preparedId) {
        this.error.set('Coșul contului are o pregătire activă. Încheie fluxul din Contul meu.');
        return;
      }
      if (cart && cart.lines.length >= 50 && !cart.lines.some((x) => x.offerId === line.offerId)) {
        this.error.set('Coșul contului acceptă maximum 50 de ambalaje distincte.');
        return;
      }
      this.selected.set({ session, cart, line: { ...line } });
    } catch (error) {
      if (generation === this.generation)
        this.error.set(
          error instanceof HttpErrorResponse && error.status === 401
            ? 'Autentifică-te în Contul meu pentru a adăuga produse în cont.'
            : error instanceof HttpErrorResponse && error.status === 410
              ? 'Coșul contului a expirat. Reîncepe-l explicit din Contul meu.'
              : errorMessage(error),
        );
    } finally {
      this.busy.set(false);
    }
  }
  async confirm() {
    const selected = this.selected();
    if (!selected || this.busy()) return;
    const generation = ++this.generation;
    this.busy.set(true);
    this.selected.set(null);
    this.error.set('');
    try {
      const cart = selected.cart ?? (await this.api.create(selected.session.id));
      if (generation !== this.generation) return;
      // A concurrently created cart must be reviewed before it can be changed.
      if (!selected.cart && (cart.lines.length || cart.customer || cart.preparedId)) {
        this.error.set(
          'Coșul contului a fost modificat. Selectează din nou destinația pentru verificare.',
        );
        return;
      }
      const lines = [
        ...cart.lines.filter((x) => x.offerId !== selected.line.offerId),
        selected.line,
      ];
      await this.api.replace(selected.session.id, cart, lines);
      if (generation === this.generation)
        this.notice.set(
          'Cantitatea a fost salvată în coșul contului ' + selected.session.email + '.',
        );
    } catch (error) {
      if (generation !== this.generation) return;
      this.error.set(
        'Nu putem confirma salvarea. ' +
          errorMessage(error) +
          ' Verifică în Contul meu înainte de a încerca din nou.',
      );
      try {
        const cart = await this.api.read(selected.session.id);
        if (generation !== this.generation) return;
        const line = cart?.lines.find((x) => x.offerId === selected.line.offerId);
        this.notice.set(
          line
            ? 'Serverul indică ' + line.packs + ' ambalaje pentru această ofertă în cont.'
            : 'Oferta nu apare în coșul contului recitit.',
        );
      } catch {
        /* An uncertain read must not be presented as an empty cart. */
      }
    } finally {
      try {
        localStorage.setItem(ACCOUNT_CART_CHANGE_KEY, crypto.randomUUID());
      } catch {
        /* Versions remain authoritative. */
      }
      this.busy.set(false);
    }
  }
}
