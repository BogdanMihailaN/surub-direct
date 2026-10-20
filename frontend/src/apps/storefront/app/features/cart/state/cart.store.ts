import { afterNextRender, computed, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { CartSelection } from '../models/cart-selection.model';
import { CART_STORAGE_KEY, parseCart } from '../data-access/cart-storage';
@Injectable({ providedIn: 'root' })
export class CartStore {
  readonly items = signal<readonly CartSelection[]>([]);
  readonly ready = signal(false);
  readonly error = signal('');
  readonly count = computed(() => this.items().length);
  constructor() {
    const destroy = inject(DestroyRef);
    afterNextRender(() => {
      const read = () => {
        try {
          this.items.set(parseCart(localStorage.getItem(CART_STORAGE_KEY)));
          this.error.set('');
        } catch {
          this.items.set([]);
          this.error.set('Coșul salvat nu poate fi citit. Poți începe un coș nou.');
        }
      };
      read();
      this.ready.set(true);
      const sync = (e: StorageEvent) => {
        if (e.key === CART_STORAGE_KEY || e.key === null) read();
      };
      window.addEventListener('storage', sync);
      destroy.onDestroy(() => window.removeEventListener('storage', sync));
    });
  }
  set(selection: CartSelection) {
    if (!this.ready()) return false;
    const items = this.items().filter((x) => x.offerId !== selection.offerId);
    if (items.length >= 50) {
      this.error.set('Coșul acceptă maximum 50 de ambalaje distincte.');
      return false;
    }
    try {
      parseCart(JSON.stringify([selection]));
    } catch {
      this.error.set('Cantitatea trebuie să fie un număr întreg pozitiv.');
      return false;
    }
    this.save([...items, selection]);
    return true;
  }
  remove(id: string) {
    this.save(this.items().filter((x) => x.offerId !== id));
  }
  clear() {
    this.save([]);
  }
  private save(items: readonly CartSelection[]) {
    this.items.set(items);
    try {
      localStorage.setItem(CART_STORAGE_KEY, JSON.stringify(items));
      this.error.set('');
    } catch {
      this.error.set(
        'Browserul nu permite salvarea coșului. Modificările rămân doar în această pagină.',
      );
    }
  }
}
