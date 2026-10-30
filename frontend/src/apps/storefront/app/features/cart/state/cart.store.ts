import { PreparationApi } from '../data-access/preparation-api.service';
import { CustomerDraft } from '../models/customer-draft.model';
import { afterNextRender, computed, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { CartSelection } from '../models/cart-selection.model';
import { StoredCart } from '../models/stored-cart.model';
import { CartPersistenceApi } from '../data-access/cart-persistence-api.service';
import { CART_STORAGE_KEY, parseCart } from '../data-access/cart-storage';
import { CART_CHANGE_KEY } from '../data-access/cart-change-key';
@Injectable({ providedIn: 'root' })
export class CartStore {
  private readonly api = inject(CartPersistenceApi);
  private readonly preparationApi = inject(PreparationApi);
  readonly preparedId = signal<string | null>(null);
  private current: StoredCart | null = null;
  private invalidated = false;
  readonly customer = signal<CustomerDraft | null>(null);
  readonly customerError = signal('');
  readonly items = signal<readonly CartSelection[]>([]);
  readonly ready = signal(false);
  readonly busy = signal(false);
  readonly stale = signal(false);
  readonly canRestart = signal(false);
  readonly legacyInvalid = signal(false);
  readonly error = signal('');
  readonly count = computed(() => this.items().length);
  readonly locked = computed(
    () => !this.ready() || this.busy() || this.stale() || !!this.preparedId(),
  );
  constructor() {
    const destroy = inject(DestroyRef);
    afterNextRender(() => {
      void this.reload();
      const sync = (event: StorageEvent) => {
        if (event.key === CART_CHANGE_KEY) {
          this.invalidate('Coșul a fost modificat în alt tab. Reîncarcă pentru a continua.');
        }
      };
      const focus = () => this.revalidate();
      window.addEventListener('storage', sync);
      window.addEventListener('focus', focus);
      destroy.onDestroy(() => {
        window.removeEventListener('storage', sync);
        window.removeEventListener('focus', focus);
      });
    });
  }
  private accept(cart: StoredCart | null) {
    if (this.invalidated) return;
    this.current = cart;
    this.preparedId.set(cart?.preparedId ?? null);
    this.customer.set(cart?.customer ?? null);
    this.items.set(cart?.lines ?? []);
  }
  private invalidate(message: string) {
    this.invalidated = true;
    this.current = null;
    this.preparedId.set(null);
    this.customer.set(null);
    this.customerError.set('');
    this.items.set([]);
    this.stale.set(true);
    this.canRestart.set(false);
    this.error.set(message);
  }
  revalidate(notifyOthers = false) {
    if (notifyOthers) this.notify();
    this.invalidate('Coșul trebuie reverificat. Reîncarcă pentru a continua.');
    // An in-flight mutation may still commit. Keep it locked until an explicit reload.
    if (!this.busy()) void this.reload();
  }
  private notify() {
    try {
      localStorage.setItem(CART_CHANGE_KEY, crypto.randomUUID());
    } catch {
      /* Server persistence remains available. */
    }
  }
  private fail(error: unknown) {
    this.invalidate('Nu putem confirma starea coșului. Reîncarcă înainte de a modifica produsele.');
    this.canRestart.set(error instanceof HttpErrorResponse && error.status === 410);
  }
  async reload(discardLegacy = false) {
    if (this.busy()) return;
    this.invalidated = false;
    this.busy.set(true);
    this.error.set('');
    this.canRestart.set(false);
    try {
      const cart = await this.api.read();
      if (this.invalidated) return;
      this.accept(cart);
      let legacy: CartSelection[];
      try {
        let raw: string | null = null;
        try {
          if (discardLegacy) localStorage.removeItem(CART_STORAGE_KEY);
          raw = localStorage.getItem(CART_STORAGE_KEY);
        } catch {
          /* A browser storage restriction must not block server persistence. */
        }
        legacy = parseCart(raw);
      } catch {
        this.legacyInvalid.set(true);
        this.stale.set(true);
        this.error.set('Coșul salvat nu poate fi citit. Poți renunța la copia locală.');
        return;
      }
      if (legacy.length) {
        const target = cart ?? (await this.api.create());
        if (this.invalidated) return;
        const merged = [
          ...target.lines,
          ...legacy.filter((x) => !target.lines.some((y) => y.offerId === x.offerId)),
        ];
        if (merged.length > 50) {
          this.legacyInvalid.set(true);
          this.stale.set(true);
          this.error.set(
            'Importul depășește 50 de ambalaje. Poți păstra coșul de pe server și renunța la copia locală.',
          );
          return;
        }
        this.accept(
          merged.length === target.lines.length ? target : await this.api.replace(target, merged),
        );
        try {
          localStorage.removeItem(CART_STORAGE_KEY);
        } catch {
          /* A later import keeps existing server lines. */
        }
        this.notify();
      }
      if (!this.invalidated) {
        this.legacyInvalid.set(false);
        this.stale.set(false);
      }
    } catch (error) {
      this.fail(error);
    } finally {
      this.ready.set(true);
      this.busy.set(false);
    }
  }
  async restart() {
    if (this.busy() || !this.canRestart()) return;
    this.invalidated = false;
    this.busy.set(true);
    try {
      this.accept(await this.api.create());
      if (this.invalidated) return;
      this.stale.set(false);
      this.canRestart.set(false);
      this.error.set('');
      this.notify();
    } catch (error) {
      this.fail(error);
    } finally {
      this.busy.set(false);
    }
  }
  async saveCustomer(customer: CustomerDraft | null) {
    if (this.locked() || !this.current) return false;
    this.busy.set(true);
    this.customerError.set('');
    try {
      this.accept(await this.api.saveCustomer(this.current, customer));
      this.notify();
      return !this.invalidated;
    } catch (error) {
      if (error instanceof HttpErrorResponse && error.status === 400)
        this.customerError.set(error.error?.detail ?? 'Verifică datele introduse.');
      else this.fail(error);
      return false;
    } finally {
      this.busy.set(false);
    }
  }
  async prepare() {
    if (this.locked() || !this.current || !this.customer()) return;
    this.busy.set(true);
    this.error.set('');
    try {
      await this.preparationApi.prepare(this.current.id, this.current.version, crypto.randomUUID());
      this.accept(await this.api.read());
      this.notify();
    } catch (error) {
      this.fail(error);
    } finally {
      this.busy.set(false);
    }
  }
  async confirmPreparation() {
    await this.orderAction('confirm');
  }
  async completePreparation() {
    await this.orderAction('complete');
  }
  private async orderAction(action: 'confirm' | 'complete') {
    const id = this.preparedId();
    if (!id || this.busy() || this.stale()) return;
    this.busy.set(true);
    this.error.set('');
    try {
      await this.preparationApi[action](id);
      this.accept(await this.api.read());
      this.notify();
    } catch (error) {
      this.fail(error);
    } finally {
      this.busy.set(false);
    }
  }
  async cancelPreparation() {
    const id = this.preparedId();
    if (!id || this.busy() || this.stale()) return;
    this.busy.set(true);
    this.error.set('');
    try {
      await this.preparationApi.cancel(id);
      this.accept(await this.api.read());
      this.notify();
    } catch (error) {
      this.fail(error);
    } finally {
      this.busy.set(false);
    }
  }
  async set(selection: CartSelection) {
    try {
      parseCart(JSON.stringify([selection]));
    } catch {
      this.error.set('Cantitatea trebuie să fie un număr întreg pozitiv.');
      return false;
    }
    return this.save((lines) => [
      ...lines.filter((x) => x.offerId !== selection.offerId),
      selection,
    ]);
  }
  remove(id: string) {
    return this.save((lines) => lines.filter((x) => x.offerId !== id));
  }
  clear() {
    return this.save(() => []);
  }
  private async save(change: (lines: readonly CartSelection[]) => readonly CartSelection[]) {
    if (this.locked()) return false;
    this.busy.set(true);
    this.error.set('');
    try {
      const target = this.current ?? (await this.api.create());
      if (this.invalidated) return false;
      const lines = change(target.lines);
      if (lines.length > 50) {
        this.error.set('Coșul acceptă maximum 50 de ambalaje distincte.');
        return false;
      }
      this.accept(await this.api.replace(target, lines));
      this.notify();
      return !this.invalidated;
    } catch (error) {
      this.fail(error);
      return false;
    } finally {
      this.busy.set(false);
    }
  }
}
