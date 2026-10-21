import { Component, effect, inject } from '@angular/core';
import { DecimalPipe, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Title } from '@angular/platform-browser';
import { CartStore } from '../../state/cart.store';
import { CartQuoteStore } from '../../state/cart-quote.store';
import { CartLine } from '../../components/cart-line/cart-line.component';
@Component({
  selector: 'sd-cart',
  imports: [RouterLink, DecimalPipe, DatePipe, CartLine],
  providers: [CartQuoteStore],
  templateUrl: './cart.component.html',
  styleUrl: './cart.component.scss',
})
export class CartPage {
  readonly cart = inject(CartStore);
  readonly pricing = inject(CartQuoteStore);
  constructor() {
    inject(Title).setTitle('Coș demonstrativ | Șurub Direct');
    effect(() => {
      const lines = this.cart.items();
      if (this.cart.ready()) void this.pricing.load(this.cart.locked() ? [] : lines);
    });
  }
  detail(id: string) {
    return this.pricing.quote()?.lines.find((x) => x.offerId === id);
  }
}
