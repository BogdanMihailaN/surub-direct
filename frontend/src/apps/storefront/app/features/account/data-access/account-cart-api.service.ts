import { inject, Injectable } from '@angular/core';
import { ApiClient } from '@shared/http/api-client.service';
import { StoredCart } from '../../cart/models/stored-cart.model';
import { CartSelection } from '../../cart/models/cart-selection.model';
@Injectable({ providedIn: 'root' })
export class AccountCartApi {
  private readonly api = inject(ApiClient);
  private path(accountId: string, suffix = '') {
    return (
      '/api/v1/ordering/account/cart' +
      suffix +
      '?expectedAccountId=' +
      encodeURIComponent(accountId)
    );
  }
  read(accountId: string) {
    return this.api.get<StoredCart | null>(this.path(accountId));
  }
  create(accountId: string) {
    return this.api.write<StoredCart>('POST', this.path(accountId, '/session'));
  }
  replace(accountId: string, cart: StoredCart, lines: readonly CartSelection[]) {
    return this.api.write<StoredCart>('PUT', this.path(accountId), {
      cartId: cart.id,
      version: cart.version,
      lines,
    });
  }
  clearCustomer(accountId: string, cart: StoredCart) {
    return this.api.write<StoredCart>('PUT', this.path(accountId, '/customer'), {
      cartId: cart.id,
      version: cart.version,
      customer: null,
    });
  }
  transfer(accountId: string, source: StoredCart, target: StoredCart, includeCustomer: boolean) {
    return this.api.write<StoredCart>('POST', this.path(accountId, '/import'), {
      cartId: source.id,
      version: source.version,
      targetCartId: target.id,
      targetVersion: target.version,
      includeCustomer,
    });
  }
}
