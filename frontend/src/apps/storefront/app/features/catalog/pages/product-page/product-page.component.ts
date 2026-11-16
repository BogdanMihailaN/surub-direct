import { ProductStore } from '../../state/product.store';
import { ProductLoading } from '../../components/product-loading/product-loading.component';
import { AddToCart } from '../../../cart/components/add-to-cart/add-to-cart.component';
import { Component, inject } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ProductGallery } from '../../components/product-gallery/product-gallery.component';
@Component({
  selector: 'sd-product',
  imports: [RouterLink, DecimalPipe, ProductGallery, AddToCart, ProductLoading],
  providers: [ProductStore],
  templateUrl: './product-page.component.html',
  styleUrl: './product-page.component.scss',
})
export class ProductPage {
  readonly store = inject(ProductStore);
  readonly item = this.store.item;
  readonly error = this.store.error;
  price(id: string) {
    return this.item()?.prices.find((x) => x.offerId === id);
  }
  available(id: string) {
    return this.item()?.stock.find((x) => x.variantId === id)?.available || 0;
  }
  packs(n: number, u: number) {
    return Math.floor(n / u);
  }
}
