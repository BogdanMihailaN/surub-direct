import { inject, Injectable, signal } from '@angular/core';
import { errorMessage } from '@shared/http/error-message';
import { CartApi } from '../data-access/cart-api.service';
import { CartQuote } from '../models/cart-quote.model';
import { CartSelection } from '../models/cart-selection.model';
@Injectable()
export class CartQuoteStore {
  private readonly api = inject(CartApi);
  private request = 0;
  readonly quote = signal<CartQuote | null>(null);
  readonly busy = signal(false);
  readonly error = signal('');
  async load(lines: readonly CartSelection[]) {
    const sequence = ++this.request;
    this.quote.set(null);
    this.error.set('');
    this.busy.set(lines.length > 0);
    if (!lines.length) return;
    try {
      const result = await this.api.quote(lines);
      if (sequence === this.request) this.quote.set(result);
    } catch (e) {
      if (sequence === this.request) this.error.set(errorMessage(e));
    } finally {
      if (sequence === this.request) this.busy.set(false);
    }
  }
}
