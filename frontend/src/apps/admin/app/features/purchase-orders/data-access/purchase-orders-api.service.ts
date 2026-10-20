import { CorrectPurchaseReceiptRequest } from '../models/correct-purchase-receipt.request';
import { ReceivePurchaseOrderRequest } from '../models/receive-purchase-order.request';
import { inject, Injectable } from '@angular/core';
import { ApiClient } from '@shared/http/api-client.service';
import { PurchaseOrder } from '../models/purchase-order.model';
import { PurchaseOrderPage } from '../models/purchase-order-page.model';
import { CreatePurchaseOrderRequest } from '../models/create-purchase-order.request';
import { OrderLineChange } from '../models/order-line-change.request';
@Injectable({ providedIn: 'root' })
export class PurchaseOrdersApi {
  private readonly api = inject(ApiClient);
  private readonly root = '/api/v1/admin/purchasing/orders';
  list(search: string, status: string, page: number) {
    return this.api.get<PurchaseOrderPage>(
      this.root + '?' + new URLSearchParams({ search, status, page: String(page) }),
    );
  }
  get(id: string) {
    return this.api.get<PurchaseOrder>(this.root + '/' + id);
  }
  create(c: CreatePurchaseOrderRequest) {
    return this.api.write<{ id: string }>('POST', this.root, c);
  }
  notes(o: PurchaseOrder, notes: string) {
    return this.api.write('PUT', this.root + '/' + o.id, { version: o.version, notes });
  }
  add(o: PurchaseOrder, supplierItemId: string, packs: number) {
    return this.api.write('POST', this.root + '/' + o.id + '/lines', {
      version: o.version,
      supplierItemId,
      packs,
    });
  }
  update(o: PurchaseOrder, lineId: string, c: OrderLineChange) {
    return this.api.write('PUT', this.root + '/' + o.id + '/lines/' + lineId, {
      ...c,
      version: o.version,
    });
  }
  remove(o: PurchaseOrder, lineId: string) {
    return this.api.write('POST', this.root + '/' + o.id + '/lines/' + lineId + '/remove', {
      version: o.version,
    });
  }
  sent(o: PurchaseOrder) {
    return this.api.write('POST', this.root + '/' + o.id + '/sent', { version: o.version });
  }
  receive(o: PurchaseOrder, c: ReceivePurchaseOrderRequest) {
    return this.api.write('POST', this.root + '/' + o.id + '/receipts', {
      ...c,
      version: o.version,
    });
  }
  retry(o: PurchaseOrder, receiptId: string) {
    return this.api.write('POST', this.root + '/' + o.id + '/receipts/' + receiptId + '/retry', {});
  }
  correct(o: PurchaseOrder, c: CorrectPurchaseReceiptRequest) {
    return this.api.write('POST', this.root + '/' + o.id + '/corrections', {
      ...c,
      version: o.version,
    });
  }
  retryCorrection(o: PurchaseOrder, id: string) {
    return this.api.write('POST', this.root + '/' + o.id + '/corrections/' + id + '/retry', {});
  }
  close(o: PurchaseOrder, reason: string) {
    return this.api.write('POST', this.root + '/' + o.id + '/close', {
      version: o.version,
      reason,
    });
  }
  cancel(o: PurchaseOrder, reason: string) {
    return this.api.write('POST', this.root + '/' + o.id + '/cancel', {
      version: o.version,
      reason,
    });
  }
}
