import { Component, computed, input, signal } from '@angular/core';
import { Product, ProductImage } from '@shared/models';
import { productImageUrl } from '@shared/catalog/product-image-url';
import { FastenerArt } from '@shared/ui/fastener-art/fastener-art.component';
@Component({
  selector: 'sd-product-gallery',
  imports: [FastenerArt],
  templateUrl: './product-gallery.component.html',
  styleUrl: './product-gallery.component.scss',
})
export class ProductGallery {
  readonly product = input.required<Product>();
  readonly selectedId = signal('');
  readonly variantId = signal('');
  readonly url = productImageUrl;
  readonly images = computed(() =>
    (this.product().images ?? []).filter(
      (x) => !this.variantId() || !x.variantId || x.variantId === this.variantId(),
    ),
  );
  readonly selected = computed<ProductImage | undefined>(
    () =>
      this.images().find((x) => x.id === this.selectedId()) ??
      this.images().find((x) => x.isPrimary) ??
      this.images()[0],
  );
  changeVariant(event: Event) {
    this.variantId.set((event.target as HTMLSelectElement).value);
    this.selectedId.set('');
  }
}
