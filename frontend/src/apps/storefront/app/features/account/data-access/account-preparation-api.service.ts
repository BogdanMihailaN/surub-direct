import { inject, Injectable } from '@angular/core';
import { ApiClient } from '@shared/http/api-client.service';
import { PreparedCart } from '../../cart/models/prepared-cart.model';
import { StoredCart } from '../../cart/models/stored-cart.model';
@Injectable({ providedIn: 'root' })
export class AccountPreparationApi {
  private readonly api = inject(ApiClient);
  private path(accountId: string, suffix = '') {
    return (
      '/api/v1/ordering/account/cart/preparation' +
      suffix +
      '?expectedAccountId=' +
      encodeURIComponent(accountId)
    );
  }
  capabilities() {
    return this.api.get<{ preparationEnabled: boolean }>('/api/v1/ordering/capabilities');
  }
  read(accountId: string) {
    return this.api.get<PreparedCart | null>(this.path(accountId));
  }
  prepare(accountId: string, cart: StoredCart) {
    return this.api.write<PreparedCart>('POST', this.path(accountId), {
      cartId: cart.id,
      version: cart.version,
      requestId: crypto.randomUUID(),
    });
  }
  act(accountId: string, id: string, action: 'cancel' | 'confirm' | 'complete') {
    return this.api.write<unknown>(
      'POST',
      this.path(accountId, '/' + encodeURIComponent(id) + '/' + action),
    );
  }
}
