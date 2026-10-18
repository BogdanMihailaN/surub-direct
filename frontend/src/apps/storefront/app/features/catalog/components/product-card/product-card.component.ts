import { Component, input } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { StoreProduct } from '@shared/models';
import { FastenerArt } from '@shared/ui/fastener-art/fastener-art.component';
@Component({
  selector: 'sd-product-card',
  imports: [RouterLink, DecimalPipe, FastenerArt],
  templateUrl: './product-card.component.html',
  styleUrl: './product-card.component.scss',
})
export class ProductCardComponent {
  readonly product = input.required<StoreProduct>();
  price(p: StoreProduct) {
    return p.prices.length ? Math.min(...p.prices.map((x) => x.grossAmount)) : null;
  }
  available(p: StoreProduct) {
    return p.product.variants.some((v) =>
      v.offers.some(
        (o) =>
          p.prices.some((x) => x.offerId === o.id) &&
          (p.stock.find((s) => s.variantId === v.id)?.available || 0) >=
            o.unitsPerPack * o.minPacks,
      ),
    );
  }
}
