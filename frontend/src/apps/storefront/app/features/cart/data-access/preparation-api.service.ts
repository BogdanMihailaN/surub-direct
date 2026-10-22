import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { PreparedCart } from '../models/prepared-cart.model';
@Injectable({ providedIn: 'root' })
export class PreparationApi {
  private readonly http = inject(HttpClient);
  private readonly path = '/api/v1/ordering/cart/preparation';
  capabilities() {
    return firstValueFrom(
      this.http.get<{ preparationEnabled: boolean }>('/api/v1/ordering/capabilities'),
    );
  }
  read() {
    return firstValueFrom(this.http.get<PreparedCart | null>(this.path));
  }
  private async headers() {
    const token = await firstValueFrom(
      this.http.get<{ token: string }>('/api/v1/ordering/cart/csrf'),
    );
    return { 'X-CART-CSRF': token.token };
  }
  async prepare(cartId: string, version: number, requestId: string) {
    return firstValueFrom(
      this.http.post<PreparedCart>(
        this.path,
        { cartId, version, requestId },
        { headers: await this.headers() },
      ),
    );
  }
  async confirm(id: string) {
    return firstValueFrom(
      this.http.post(this.path + '/' + id + '/confirm', {}, { headers: await this.headers() }),
    );
  }
  async complete(id: string) {
    return firstValueFrom(
      this.http.post<void>(
        this.path + '/' + id + '/complete',
        {},
        { headers: await this.headers() },
      ),
    );
  }
  async cancel(id: string) {
    return firstValueFrom(
      this.http.post<void>(this.path + '/' + id + '/cancel', {}, { headers: await this.headers() }),
    );
  }
}
