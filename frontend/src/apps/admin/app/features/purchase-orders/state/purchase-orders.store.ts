import { ReceivePurchaseOrderRequest } from '../models/receive-purchase-order.request';
import { inject, Injectable, signal } from '@angular/core';
import { errorMessage } from '@shared/http/error-message';
import { PurchasingApi } from '../../purchasing/data-access/purchasing-api.service';
import { Supplier } from '../../purchasing/models/supplier.model';
import { PurchaseOrdersApi } from '../data-access/purchase-orders-api.service';
import { PurchaseOrder } from '../models/purchase-order.model';
import { PurchaseOrderPage } from '../models/purchase-order-page.model';
import { CreatePurchaseOrderRequest } from '../models/create-purchase-order.request';
import { OrderLineChange } from '../models/order-line-change.request';
@Injectable()
export class PurchaseOrdersStore {
  private readonly api = inject(PurchaseOrdersApi);
  private readonly suppliersApi = inject(PurchasingApi);
  readonly data = signal<PurchaseOrderPage>({ items: [], total: 0, page: 1, pageSize: 25 });
  readonly order = signal<PurchaseOrder | null>(null);
  readonly suppliers = signal<Supplier[]>([]);
  readonly supplier = signal<Supplier | null>(null);
  readonly busy = signal(false);
  readonly stale = signal(false);
  readonly error = signal('');
  readonly message = signal('');
  private search = '';
  private status = '';
  load(page = 1) {
    return this.run(async () => {
      this.data.set(await this.api.list(this.search, this.status, page));
    });
  }
  filter(value: { search: string; status: string }) {
    if (this.busy()) return;
    this.search = value.search;
    this.status = value.status;
    return this.load();
  }
  findSuppliers(search: string) {
    return this.run(async () => {
      this.suppliers.set((await this.suppliersApi.list(search, 'true', 1)).items);
    });
  }
  newOrder() {
    if (this.busy()) return;
    this.order.set(null);
    this.supplier.set(null);
    this.stale.set(false);
    this.message.set('');
    this.error.set('');
    return this.findSuppliers('');
  }
  select(id: string) {
    return this.run(() => this.read(id));
  }
  private async read(id: string) {
    const o = await this.api.get(id);
    this.order.set(o);
    this.supplier.set(null);
    this.supplier.set(await this.suppliersApi.get(o.supplierId));
    this.stale.set(false);
  }
  create(c: CreatePurchaseOrderRequest) {
    return this.run(async () => {
      const result = await this.api.create(c);
      await this.read(result.id);
      await this.refresh();
    }, 'Comanda a fost creată ca ciornă.');
  }
  notes(notes: string) {
    return this.mutate((o) => this.api.notes(o, notes), 'Observațiile au fost salvate.');
  }
  add(c: { supplierItemId: string; packs: number }) {
    return this.mutate(
      (o) => this.api.add(o, c.supplierItemId, c.packs),
      'Articolul a fost adăugat.',
    );
  }
  update(id: string, c: OrderLineChange) {
    return this.mutate((o) => this.api.update(o, id, c), 'Linia a fost actualizată.');
  }
  remove(id: string) {
    return this.mutate((o) => this.api.remove(o, id), 'Linia a fost eliminată.');
  }
  sent() {
    return this.mutate((o) => this.api.sent(o), 'Comanda a fost marcată manual ca trimisă.');
  }
  receive(c: ReceivePurchaseOrderRequest) {
    return this.mutate((o) => this.api.receive(o, c), 'Recepția a fost confirmată în stoc.');
  }
  retry(receiptId: string) {
    return this.mutate((o) => this.api.retry(o, receiptId), 'Recepția a fost confirmată în stoc.');
  }
  cancel(reason: string) {
    return this.mutate(
      (o) => this.api.cancel(o, reason),
      'Anularea a fost înregistrată. Furnizorul nu a fost notificat automat.',
    );
  }
  private refresh() {
    return this.api.list(this.search, this.status, this.data().page).then((p) => this.data.set(p));
  }
  private mutate(operation: (o: PurchaseOrder) => Promise<unknown>, message: string) {
    if (this.stale()) return;
    return this.run(async () => {
      const o = this.order();
      if (!o) return;
      try {
        await operation(o);
        await this.read(o.id);
        await this.refresh();
      } catch (error) {
        this.stale.set(true);
        throw error;
      }
    }, message);
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
