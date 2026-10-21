import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { StoredCart } from '../models/stored-cart.model';
import { CartSelection } from '../models/cart-selection.model';
@Injectable({ providedIn: 'root' })
export class CartPersistenceApi {
  private readonly http = inject(HttpClient);
  private readonly path = '/api/v1/ordering/cart';
  read() {
    return firstValueFrom(this.http.get<StoredCart | null>(this.path));
  }
  private async headers() {
    const token = await firstValueFrom(this.http.get<{ token: string }>(this.path + '/csrf'));
    return { 'X-CART-CSRF': token.token };
  }
  async create() {
    return firstValueFrom(
      this.http.post<StoredCart>(this.path + '/session', {}, { headers: await this.headers() }),
    );
  }
  async replace(cart: StoredCart, lines: readonly CartSelection[]) {
    return firstValueFrom(
      this.http.put<StoredCart>(
        this.path,
        { cartId: cart.id, version: cart.version, lines },
        { headers: await this.headers() },
      ),
    );
  }
}
