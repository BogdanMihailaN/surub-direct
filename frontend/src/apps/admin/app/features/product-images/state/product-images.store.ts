import { inject, Injectable, signal } from '@angular/core';
import { Product, ProductImage } from '@shared/models';
import { errorMessage } from '@shared/http/error-message';
import { ProductImagesApi } from '../data-access/product-images-api.service';
import { ImageUploadRequest } from '../models/image-upload.request';
import { ImageDetailsRequest } from '../models/image-details.request';
@Injectable()
export class ProductImagesStore {
  private readonly api = inject(ProductImagesApi);
  private productId = '';
  readonly images = signal<ProductImage[]>([]);
  readonly busy = signal(false);
  readonly error = signal('');
  readonly message = signal('');
  select(product: Product) {
    if (this.productId !== product.id) {
      this.error.set('');
      this.message.set('');
    }
    this.productId = product.id;
    this.images.set(product.images ?? []);
  }
  upload(request: ImageUploadRequest) {
    return this.run((id) => this.api.upload(id, request), 'Imaginea a fost încărcată.');
  }
  update(id: string, request: ImageDetailsRequest) {
    return this.run(
      (productId) => this.api.update(productId, id, request),
      'Detaliile imaginii au fost salvate.',
    );
  }
  primary(id: string) {
    const ids = this.images().map((x) => x.id);
    return this.run(
      (productId) => this.api.arrange(productId, ids, id),
      'Imaginea principală a fost schimbată.',
    );
  }
  move(id: string, direction: number) {
    const ids = this.images().map((x) => x.id);
    const index = ids.indexOf(id);
    const target = index + direction;
    if (index < 0 || target < 0 || target >= ids.length) return Promise.resolve(null);
    [ids[index], ids[target]] = [ids[target], ids[index]];
    const primary = this.images().find((x) => x.isPrimary)!.id;
    return this.run(
      (productId) => this.api.arrange(productId, ids, primary),
      'Ordinea imaginilor a fost salvată.',
    );
  }
  remove(id: string) {
    return this.run((productId) => this.api.remove(productId, id), 'Imaginea a fost eliminată.');
  }
  private async run(
    operation: (id: string) => Promise<unknown>,
    message: string,
  ): Promise<string | null> {
    if (this.busy()) return null;
    const id = this.productId;
    this.busy.set(true);
    this.error.set('');
    this.message.set('');
    try {
      await operation(id);
      const item = await this.api.get(id);
      if (id === this.productId) {
        this.images.set(item.product.images ?? []);
        this.message.set(message);
      }
      return id;
    } catch (error) {
      if (id === this.productId) this.error.set(errorMessage(error));
      return null;
    } finally {
      this.busy.set(false);
    }
  }
}
