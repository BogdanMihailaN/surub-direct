import { inject, Injectable } from '@angular/core';
import { ApiClient } from '@shared/http/api-client.service';
import { Supplier } from '../models/supplier.model';
import { SupplierPage } from '../models/supplier-page.model';
import { SupplierDetails } from '../models/supplier-details.model';
import { SupplierItemDetails } from '../models/supplier-item-details.model';
import { CatalogVariant } from '../models/catalog-variant.model';
@Injectable({ providedIn: 'root' })
export class PurchasingApi {
  private readonly api = inject(ApiClient);
  private readonly root = '/api/v1/admin/purchasing';
  list(search: string, active: string, page: number) {
    const query = new URLSearchParams({ search, page: String(page) });
    if (active) query.set('active', active);
    return this.api.get<SupplierPage>(this.root + '/suppliers?' + query);
  }
  get(id: string) {
    return this.api.get<Supplier>(this.root + '/suppliers/' + id);
  }
  create(details: SupplierDetails) {
    return this.api.write<{ id: string }>('POST', this.root + '/suppliers', details);
  }
  update(id: string, details: SupplierDetails) {
    return this.api.write('PUT', this.root + '/suppliers/' + id, details);
  }
  searchVariants(search: string) {
    return this.api.get<CatalogVariant[]>(
      this.root + '/variants?search=' + encodeURIComponent(search),
    );
  }
  addItem(id: string, variantId: string, details: SupplierItemDetails) {
    return this.api.write('POST', this.root + '/suppliers/' + id + '/items', {
      ...details,
      variantId,
    });
  }
  updateItem(supplierId: string, id: string, details: SupplierItemDetails) {
    return this.api.write('PUT', this.root + '/suppliers/' + supplierId + '/items/' + id, details);
  }
}
