import { Component, input, output } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { CartQuote } from '../../models/cart-quote.model';
@Component({
  selector: 'sd-cart-summary',
  imports: [DatePipe, DecimalPipe],
  templateUrl: './cart-summary.component.html',
  styleUrl: './cart-summary.component.scss',
})
export class CartSummary {
  readonly quote = input<CartQuote | null>(null);
  readonly busy = input(false);
  readonly locked = input(false);
  readonly error = input('');
  readonly refreshed = output<void>();
}
