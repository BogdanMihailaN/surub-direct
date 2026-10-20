import { OrderCorrections } from '../../components/order-corrections/order-corrections.component';
import { OrderReceipts } from '../../components/order-receipts/order-receipts.component';
import { Component, inject } from '@angular/core';
import { PurchaseOrdersStore } from '../../state/purchase-orders.store';
import { OrderList } from '../../components/order-list/order-list.component';
import { OrderCreate } from '../../components/order-create/order-create.component';
import { OrderNotes } from '../../components/order-notes/order-notes.component';
import { OrderLineAdd } from '../../components/order-line-add/order-line-add.component';
import { OrderLines } from '../../components/order-lines/order-lines.component';
import { OrderActions } from '../../components/order-actions/order-actions.component';
@Component({
  selector: 'sd-purchase-orders',
  imports: [
    OrderReceipts,
    OrderCorrections,
    OrderList,
    OrderCreate,
    OrderNotes,
    OrderLineAdd,
    OrderLines,
    OrderActions,
  ],
  providers: [PurchaseOrdersStore],
  templateUrl: './purchase-orders.component.html',
  styleUrl: './purchase-orders.component.scss',
})
export class PurchaseOrdersPage {
  readonly store = inject(PurchaseOrdersStore);
  constructor() {
    void this.initialize();
  }
  private async initialize() {
    await this.store.load();
    await this.store.findSuppliers('');
  }
}
