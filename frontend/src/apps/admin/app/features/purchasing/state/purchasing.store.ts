import { inject, Injectable, signal } from '@angular/core';
import { errorMessage } from '@shared/http/error-message';
import { PurchasingApi } from '../data-access/purchasing-api.service';
import { Supplier } from '../models/supplier.model';
import { SupplierDetails } from '../models/supplier-details.model';
import { SupplierItemDetails } from '../models/supplier-item-details.model';
import { SupplierItem } from '../models/supplier-item.model';
import { SupplierPage } from '../models/supplier-page.model';
import { CatalogVariant } from '../models/catalog-variant.model';
@Injectable()
export class PurchasingStore {
  private readonly api = inject(PurchasingApi);
  readonly page = signal<SupplierPage>({ items: [], total: 0, page: 1, pageSize: 25 });
  readonly supplier = signal<Supplier | null>(null);
  readonly editingItem = signal<SupplierItem | null>(null);
  readonly variant = signal<CatalogVariant | null>(null);
  readonly variants = signal<CatalogVariant[]>([]);
  readonly searched = signal(false);
  readonly busy = signal(false);
  readonly error = signal('');
  readonly message = signal('');
  private search = '';
  private active = '';
  load(page = 1) {
    return this.run(async () => this.page.set(await this.api.list(this.search, this.active, page)));
  }
  filter(value: { search: string; active: string }) {
    if (this.busy()) return;
    this.search = value.search;
    this.active = value.active;
    return this.load();
  }
  newSupplier() {
    if (this.busy()) return;
    this.supplier.set(null);
    this.resetItem();
    this.error.set('');
    this.message.set('');
  }
  select(id: string) {
    return this.run(async () => {
      this.supplier.set(await this.api.get(id));
      this.resetItem();
    });
  }
  save(details: SupplierDetails) {
    return this.run(async () => {
      const current = this.supplier();
      const id = current ? current.id : (await this.api.create(details)).id;
      if (current) await this.api.update(id, details);
      this.supplier.set(await this.api.get(id));
      this.resetItem();
      this.page.set(await this.api.list(this.search, this.active, this.page().page));
    }, 'Furnizorul a fost salvat.');
  }
  findVariants(search: string) {
    return this.run(async () => {
      this.variants.set([]);
      this.searched.set(false);
      this.variants.set(await this.api.searchVariants(search));
      this.searched.set(true);
    });
  }
  chooseVariant(variant: CatalogVariant) {
    if (this.busy()) return;
    this.variant.set(variant);
    this.editingItem.set(null);
  }
  editItem(item: SupplierItem) {
    if (this.busy()) return;
    this.editingItem.set(item);
    this.variant.set(null);
  }
  resetItem() {
    this.editingItem.set(null);
    this.variant.set(null);
    this.variants.set([]);
    this.searched.set(false);
  }
  cancelItem() {
    if (!this.busy()) this.resetItem();
  }
  saveItem(details: SupplierItemDetails) {
    return this.run(async () => {
      const supplier = this.supplier();
      if (!supplier?.isActive) return;
      const item = this.editingItem();
      if (item) await this.api.updateItem(supplier.id, item.id, details);
      else {
        const variant = this.variant();
        if (!variant) return;
        await this.api.addItem(supplier.id, variant.id, details);
      }
      this.supplier.set(await this.api.get(supplier.id));
      this.resetItem();
    }, 'Asocierea SKU a fost salvată.');
  }
  private async run(operation: () => Promise<void>, message = '') {
    if (this.busy()) return;
    this.busy.set(true);
    this.error.set('');
    this.message.set('');
    try {
      await operation();
      this.message.set(message);
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.busy.set(false);
    }
  }
}
