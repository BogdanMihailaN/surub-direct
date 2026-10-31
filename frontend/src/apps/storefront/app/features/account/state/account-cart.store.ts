import { afterNextRender, computed, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { errorMessage } from '@shared/http/error-message';
import { CART_CHANGE_KEY } from '../../cart/data-access/cart-change-key';
import { CartPersistenceApi } from '../../cart/data-access/cart-persistence-api.service';
import { StoredCart } from '../../cart/models/stored-cart.model';
import { CartSelection } from '../../cart/models/cart-selection.model';
import { CartStore } from '../../cart/state/cart.store';
import { AccountCartApi } from '../data-access/account-cart-api.service';
import { CartTransferSelection } from '../models/cart-transfer-selection';
import { AccountPreparationApi } from '../data-access/account-preparation-api.service';
const CHANGE_KEY = 'surub-direct.account-cart.changed';
@Injectable()
export class AccountCartStore {
  private readonly api = inject(AccountCartApi);
  private readonly preparationApi = inject(AccountPreparationApi);
  private readonly browserApi = inject(CartPersistenceApi);
  private readonly browserCart = inject(CartStore);
  private accountId = '';
  private generation = 0;
  readonly account = signal<StoredCart | null>(null);
  readonly browser = signal<StoredCart | null>(null);
  readonly accountStatus = signal<'loading' | 'ready' | 'expired' | 'error'>('loading');
  readonly browserReady = signal(false);
  readonly accountError = signal('');
  readonly browserError = signal('');
  readonly error = signal('');
  readonly notice = signal('');
  readonly busy = signal(false);
  readonly stale = signal(false);
  readonly transfer = signal<CartTransferSelection | null>(null);
  readonly restarting = signal(false);
  readonly locked = computed(
    () =>
      this.busy() ||
      this.stale() ||
      this.accountStatus() !== 'ready' ||
      !!this.account()?.preparedId,
  );
  readonly canTransfer = computed(
    () =>
      !this.locked() &&
      !!this.account() &&
      !this.account()!.lines.length &&
      !this.account()!.customer &&
      this.browserReady() &&
      !!this.browser()?.lines.length &&
      !this.browser()?.preparedId,
  );
  constructor() {
    const destroy = inject(DestroyRef);
    destroy.onDestroy(() => {
      this.generation++;
    });
    afterNextRender(() => {
      const sync = (event: StorageEvent) => {
        if (event.key === CHANGE_KEY || event.key === CART_CHANGE_KEY || event.key === null) {
          this.generation++;
          this.account.set(null);
          this.browser.set(null);
          this.transfer.set(null);
          this.restarting.set(false);
          this.stale.set(true);
          this.notice.set('');
          this.error.set('Un coș s-a modificat în alt tab. Reîncarcă înainte de a continua.');
        }
      };
      window.addEventListener('storage', sync);
      destroy.onDestroy(() => window.removeEventListener('storage', sync));
    });
  }
  private async refresh(generation: number) {
    this.account.set(null);
    this.browser.set(null);
    this.accountStatus.set('loading');
    this.browserReady.set(false);
    this.accountError.set('');
    this.browserError.set('');
    const [account, browser] = await Promise.allSettled([
      this.api.read(this.accountId),
      this.browserApi.read(),
    ]);
    if (generation !== this.generation) return;
    if (account.status === 'fulfilled') {
      this.account.set(account.value);
      this.accountStatus.set('ready');
    } else {
      this.accountStatus.set(
        account.reason instanceof HttpErrorResponse && account.reason.status === 410
          ? 'expired'
          : 'error',
      );
      this.accountError.set(errorMessage(account.reason));
    }
    if (browser.status === 'fulfilled') {
      this.browser.set(browser.value);
      this.browserReady.set(true);
    } else this.browserError.set(errorMessage(browser.reason));
  }
  async load(accountId: string) {
    if (this.busy() || !accountId) return;
    this.accountId = accountId;
    const generation = ++this.generation;
    this.busy.set(true);
    this.stale.set(false);
    this.transfer.set(null);
    this.restarting.set(false);
    this.error.set('');
    this.notice.set('');
    try {
      await this.refresh(generation);
    } finally {
      this.busy.set(false);
    }
  }
  selectTransfer() {
    if (!this.canTransfer()) return;
    this.transfer.set({ source: this.browser()!, target: this.account()! });
    this.error.set('');
    this.notice.set('');
  }
  private announce() {
    try {
      localStorage.setItem(CHANGE_KEY, crypto.randomUUID());
    } catch {
      /* Server versions remain authoritative when browser storage is restricted. */
    }
  }
  private async mutate(operation: () => Promise<unknown>, message: string, transfer = false) {
    if (this.busy() || this.stale() || !this.accountId) return;
    const generation = ++this.generation;
    this.busy.set(true);
    this.transfer.set(null);
    this.restarting.set(false);
    this.error.set('');
    this.notice.set('');
    try {
      let failure: unknown;
      try {
        await operation();
      } catch (e) {
        failure = e;
      }
      this.announce();
      if (transfer) this.browserCart.revalidate(true);
      if (generation !== this.generation) return;
      if (failure)
        this.error.set(
          'Nu putem confirma operațiunea. ' +
            errorMessage(failure) +
            ' Verifică starea recitită înainte de a încerca din nou.',
        );
      else this.notice.set(message);
      await this.refresh(generation);
    } finally {
      this.busy.set(false);
    }
  }
  prepare() {
    const cart = this.account();
    if (!cart || this.locked() || !cart.customer || !cart.lines.length) return;
    return this.mutate(
      () => this.preparationApi.prepare(this.accountId, cart),
      'Pregătirea a fost solicitată.',
    );
  }
  preparationAction(id: string, action: 'cancel' | 'confirm' | 'complete') {
    if (this.account()?.preparedId !== id || this.accountStatus() !== 'ready') return;
    return this.mutate(
      () => this.preparationApi.act(this.accountId, id, action),
      'Operațiunea a fost înregistrată. Actualizează istoricul comenzilor pentru rezultat.',
    );
  }
  create() {
    if (this.locked() || this.account()) return;
    return this.mutate(() => this.api.create(this.accountId), 'Coșul contului a fost creat.');
  }
  selectRestart() {
    if (!this.busy() && !this.stale() && this.accountStatus() === 'expired')
      this.restarting.set(true);
  }
  restart() {
    if (!this.restarting()) return;
    return this.mutate(() => this.api.create(this.accountId), 'Coșul contului a fost reînceput.');
  }
  confirmTransfer(includeCustomer: boolean) {
    const selected = this.transfer();
    if (!selected || !this.canTransfer()) return;
    return this.mutate(
      () => this.api.transfer(this.accountId, selected.source, selected.target, includeCustomer),
      'Produsele au fost transferate în coșul contului. Coșul browserului este acum gol.',
      true,
    );
  }
  set(selection: CartSelection) {
    const cart = this.account();
    if (!cart || this.locked()) return;
    const lines = cart.lines.map((line) => (line.offerId === selection.offerId ? selection : line));
    return this.mutate(
      () => this.api.replace(this.accountId, cart, lines),
      'Cantitatea a fost actualizată.',
    );
  }
  remove(offerId: string) {
    const cart = this.account();
    if (!cart || this.locked()) return;
    return this.mutate(
      () =>
        this.api.replace(
          this.accountId,
          cart,
          cart.lines.filter((line) => line.offerId !== offerId),
        ),
      'Articolul a fost eliminat.',
    );
  }
  clearCustomer() {
    const cart = this.account();
    if (!cart || this.locked() || !cart.customer) return;
    return this.mutate(
      () => this.api.clearCustomer(this.accountId, cart),
      'Datele clientului au fost șterse din coșul contului.',
    );
  }
}
