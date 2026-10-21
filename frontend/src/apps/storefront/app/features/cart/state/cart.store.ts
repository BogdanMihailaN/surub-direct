import { CustomerDraft } from '../models/customer-draft.model';
import { afterNextRender, computed, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { CartSelection } from '../models/cart-selection.model';
import { StoredCart } from '../models/stored-cart.model';
import { CartPersistenceApi } from '../data-access/cart-persistence-api.service';
import { CART_STORAGE_KEY, parseCart } from '../data-access/cart-storage';
const CHANGE_KEY = 'surub-direct.cart.changed';
@Injectable({ providedIn: 'root' })
export class CartStore {
  private readonly api = inject(CartPersistenceApi);
  private current: StoredCart | null = null;
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
  readonly locked = computed(() => !this.ready() || this.busy() || this.stale());
  constructor() {
    const destroy = inject(DestroyRef);
    afterNextRender(() => {
      void this.reload();
      const sync = (event: StorageEvent) => {
        if (event.key === CHANGE_KEY) {
          this.stale.set(true);
          this.error.set('Coșul a fost modificat în alt tab. Reîncarcă pentru a continua.');
        }
      };
      window.addEventListener('storage', sync);
      destroy.onDestroy(() => window.removeEventListener('storage', sync));
    });
  }
  private accept(cart: StoredCart | null) {
    this.current = cart;
    this.customer.set(cart?.customer ?? null);
    this.items.set(cart?.lines ?? []);
  }
  private notify() {
    try {
      localStorage.setItem(CHANGE_KEY, crypto.randomUUID());
    } catch {
      /* Server persistence remains available. */
    }
  }
  private fail(error: unknown) {
    this.stale.set(true);
    this.canRestart.set(error instanceof HttpErrorResponse && error.status === 410);
    this.error.set('Nu putem confirma starea coșului. Reîncarcă înainte de a modifica produsele.');
  }
  async reload(discardLegacy = false) {
    if (this.busy()) return;
    this.busy.set(true);
    this.error.set('');
    this.canRestart.set(false);
    try {
      const cart = await this.api.read();
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
      this.legacyInvalid.set(false);
      this.stale.set(false);
    } catch (error) {
      this.fail(error);
    } finally {
      this.ready.set(true);
      this.busy.set(false);
    }
  }
  async restart() {
    if (this.busy() || !this.canRestart()) return;
    this.busy.set(true);
    try {
      this.accept(await this.api.create());
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
      return true;
    } catch (error) {
      if (error instanceof HttpErrorResponse && error.status === 400)
        this.customerError.set(error.error?.detail ?? 'Verifică datele introduse.');
      else this.fail(error);
      return false;
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
      const lines = change(target.lines);
      if (lines.length > 50) {
        this.error.set('Coșul acceptă maximum 50 de ambalaje distincte.');
        return false;
      }
      this.accept(await this.api.replace(target, lines));
      this.notify();
      return true;
    } catch (error) {
      this.fail(error);
      return false;
    } finally {
      this.busy.set(false);
    }
  }
}
