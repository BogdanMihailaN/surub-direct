import { inject, Injectable } from '@angular/core';
import { ApiClient } from '@shared/http/api-client.service';
import { StoreProduct } from '@shared/models';
import { ImageDetailsRequest } from '../models/image-details.request';
import { ImageUploadRequest } from '../models/image-upload.request';
@Injectable({ providedIn: 'root' })
export class ProductImagesApi {
  private readonly api = inject(ApiClient);
  private path(productId: string) {
    return '/api/v1/admin/products/' + productId;
  }
  get(productId: string) {
    return this.api.get<StoreProduct>(this.path(productId));
  }
  upload(productId: string, request: ImageUploadRequest) {
    const params = new URLSearchParams({ altText: request.altText });
    if (request.variantId) params.set('variantId', request.variantId);
    return this.api.write<{ id: string }>(
      'POST',
      this.path(productId) + '/images?' + params,
      request.file,
    );
  }
  update(productId: string, id: string, request: ImageDetailsRequest) {
    return this.api.write('PUT', this.path(productId) + '/images/' + id, request);
  }
  arrange(productId: string, imageIds: string[], primaryId: string) {
    return this.api.write('PUT', this.path(productId) + '/images/order', { imageIds, primaryId });
  }
  remove(productId: string, id: string) {
    return this.api.write('DELETE', this.path(productId) + '/images/' + id);
  }
}
