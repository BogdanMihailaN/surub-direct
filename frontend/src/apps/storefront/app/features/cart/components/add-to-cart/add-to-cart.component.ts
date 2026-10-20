import { Component, effect, inject, input, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Offer } from '@shared/models/offer.model';
import { CartStore } from '../../state/cart.store';
@Component({
  selector: 'sd-add-to-cart',
  imports: [FormsModule, RouterLink],
  templateUrl: './add-to-cart.component.html',
  styleUrl: './add-to-cart.component.scss',
})
export class AddToCart {
  readonly slug = input.required<string>();
  readonly offer = input.required<Offer>();
  readonly available = input(0);
  readonly priced = input(false);
  readonly cart = inject(CartStore);
  readonly message = signal('');
  packs = 1;
  constructor() {
    effect(() => {
      const o = this.offer();
      this.packs = this.cart.items().find((x) => x.offerId === o.id)?.packs ?? o.minPacks;
    });
  }
  existing() {
    return this.cart.items().some((x) => x.offerId === this.offer().id);
  }
  submit(form: NgForm) {
    const o = this.offer();
    if (
      form.invalid ||
      !this.priced() ||
      !Number.isInteger(this.packs) ||
      this.packs < o.minPacks ||
      (this.packs - o.minPacks) % o.packStep !== 0 ||
      this.packs * o.unitsPerPack > this.available()
    )
      return;
    this.cart.set({ productSlug: this.slug(), offerId: o.id, packs: this.packs });
    this.message.set(this.cart.error() || 'Cantitatea este în coș.');
  }
}
