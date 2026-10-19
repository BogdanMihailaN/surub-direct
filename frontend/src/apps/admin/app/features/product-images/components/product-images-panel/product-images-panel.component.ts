import { Component, effect, inject, input, output } from '@angular/core';
import { Product } from '@shared/models';
import { ProductImagesStore } from '../../state/product-images.store';
import { ImageUploadForm } from '../image-upload-form/image-upload-form.component';
import { ImageEditor } from '../image-editor/image-editor.component';
@Component({
  selector: 'sd-product-images-panel',
  imports: [ImageUploadForm, ImageEditor],
  providers: [ProductImagesStore],
  templateUrl: './product-images-panel.component.html',
  styleUrl: './product-images-panel.component.scss',
})
export class ProductImagesPanel {
  readonly product = input.required<Product>();
  readonly disabled = input(false);
  readonly updated = output<string>();
  readonly store = inject(ProductImagesStore);
  constructor() {
    effect(() => this.store.select(this.product()));
  }
  async done(operation: Promise<string | null>) {
    const id = await operation;
    if (id) this.updated.emit(id);
  }
}
