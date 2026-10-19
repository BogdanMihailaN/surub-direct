import { Component, effect, input, output, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { ProductImage, Variant } from '@shared/models';
import { productImageUrl } from '@shared/catalog/product-image-url';
import { ImageDetailsRequest } from '../../models/image-details.request';
@Component({
  selector: 'sd-image-editor',
  imports: [FormsModule],
  templateUrl: './image-editor.component.html',
  styleUrl: './image-editor.component.scss',
})
export class ImageEditor {
  readonly image = input.required<ProductImage>();
  readonly productId = input.required<string>();
  readonly variants = input.required<Variant[]>();
  readonly disabled = input(false);
  readonly first = input(false);
  readonly last = input(false);
  readonly saved = output<ImageDetailsRequest>();
  readonly primary = output<void>();
  readonly moved = output<number>();
  readonly removed = output<void>();
  readonly confirming = signal(false);
  readonly url = productImageUrl;
  details: ImageDetailsRequest = { altText: '', variantId: null };
  constructor() {
    effect(() => {
      const image = this.image();
      this.details = { altText: image.altText, variantId: image.variantId };
      this.confirming.set(false);
    });
  }
  submit(form: NgForm) {
    if (!this.disabled() && form.valid) this.saved.emit({ ...this.details });
  }
}
