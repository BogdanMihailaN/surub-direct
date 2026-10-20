import { Component, input, output, effect } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CartSelection } from '../../models/cart-selection.model';
import { CartQuoteLine } from '../../models/cart-quote-line.model';
@Component({
  selector: 'sd-cart-line',
  imports: [FormsModule, RouterLink, DecimalPipe],
  templateUrl: './cart-line.component.html',
  styleUrl: './cart-line.component.scss',
})
export class CartLine {
  readonly selection = input.required<CartSelection>();
  readonly detail = input<CartQuoteLine>();
  readonly changed = output<CartSelection>();
  readonly removed = output<string>();
  packs = 1;
  constructor() {
    effect(() => {
      this.packs = this.selection().packs;
    });
  }
  submit(form: NgForm) {
    if (form.invalid || !Number.isInteger(this.packs) || this.packs < 1) return;
    this.changed.emit({ ...this.selection(), packs: this.packs });
  }
}
