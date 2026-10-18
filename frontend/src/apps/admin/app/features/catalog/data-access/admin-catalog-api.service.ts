import { inject, Injectable } from '@angular/core';
import { ApiClient } from '@shared/http/api-client.service';
import { StoreProduct, StorePage } from '@shared/models';
import { SetVariantAttributeRequest } from '../models/set-variant-attribute.request';
import { UpdateVariantRequest } from '../models/update-variant.request';
import { UpdateOfferRequest } from '../models/update-offer.request';
import { UpdateProductRequest } from '../models/update-product.request';
import { CreateProductRequest } from '../models/create-product.request';
import { AddVariantRequest } from '../models/add-variant.request';
import { AddOfferRequest } from '../models/add-offer.request';
import { SetPriceRequest } from '../models/set-price.request';
import { ReceiveStockRequest } from '../models/receive-stock.request';
@Injectable({ providedIn: 'root' })
export class AdminCatalogApi {
  private readonly api = inject(ApiClient);
  list(page: number, search: string, status: string) {
    const query = new URLSearchParams({ page: String(page), pageSize: '25' });
    if (search) query.set('search', search);
    if (status) query.set('status', status);
    return this.api.get<StorePage>(`/api/v1/admin/products?${query}`);
  }
  get(id: string) {
    return this.api.get<StoreProduct>(`/api/v1/admin/products/${id}`);
  }
  update(id: string, command: UpdateProductRequest) {
    return this.api.write('PUT', `/api/v1/admin/products/${id}`, command);
  }
  archive(id: string) {
    return this.api.write('POST', `/api/v1/admin/products/${id}/archive`);
  }
  restore(id: string) {
    return this.api.write('POST', `/api/v1/admin/products/${id}/restore`);
  }
  create(command: CreateProductRequest) {
    return this.api.write<{ id: string }>('POST', '/api/v1/admin/products', command);
  }
  addVariant(id: string, command: AddVariantRequest) {
    return this.api.write<{ id: string }>('POST', `/api/v1/admin/products/${id}/variants`, command);
  }
  addOffer(id: string, command: AddOfferRequest) {
    const { variantId, ...body } = command;
    return this.api.write<{ id: string }>(
      'POST',
      `/api/v1/admin/products/${id}/variants/${variantId}/offers`,
      body,
    );
  }
  setAttributes(id: string, variantId: string, command: SetVariantAttributeRequest[]) {
    return this.api.write(
      'PUT',
      `/api/v1/admin/products/${id}/variants/${variantId}/attributes`,
      command,
    );
  }
  updateVariant(id: string, variantId: string, command: UpdateVariantRequest) {
    return this.api.write('PUT', `/api/v1/admin/products/${id}/variants/${variantId}`, command);
  }
  updateOffer(id: string, variantId: string, offerId: string, command: UpdateOfferRequest) {
    return this.api.write<{ packSizeChanged: boolean }>(
      'PUT',
      `/api/v1/admin/products/${id}/variants/${variantId}/offers/${offerId}`,
      command,
    );
  }
  setPrice(command: SetPriceRequest) {
    const { offerId, ...body } = command;
    return this.api.write('PUT', `/api/v1/admin/offers/${offerId}/price`, body);
  }
  receive(command: ReceiveStockRequest) {
    const { variantId, ...body } = command;
    return this.api.write('POST', `/api/v1/admin/variants/${variantId}/receipts`, body);
  }
  publish(id: string) {
    return this.api.write('POST', `/api/v1/admin/products/${id}/publish`);
  }
}
