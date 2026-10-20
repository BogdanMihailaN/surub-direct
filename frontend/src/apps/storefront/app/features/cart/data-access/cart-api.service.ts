import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { API_ORIGIN } from '@shared/http/api-origin.token';
import { CartSelection } from '../models/cart-selection.model';
import { CartQuote } from '../models/cart-quote.model';
@Injectable({ providedIn: 'root' })
export class CartApi {
  private readonly http = inject(HttpClient);
  private readonly origin = inject(API_ORIGIN);
  quote(lines: readonly CartSelection[]) {
    return firstValueFrom(
      this.http.post<CartQuote>(this.origin + '/api/v1/ordering/cart/quote', { lines }),
    );
  }
}
