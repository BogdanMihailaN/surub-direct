import { Component, input, output, signal, effect } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { PurchaseOrder } from '../../models/purchase-order.model';
import { OrderLineChange } from '../../models/order-line-change.request';
import { OrderLineEditor } from '../order-line-editor/order-line-editor.component';
@Component({
  selector: 'sd-order-lines',
  imports: [DecimalPipe, OrderLineEditor],
  templateUrl: './order-lines.component.html',
  styleUrl: './order-lines.component.scss',
})
export class OrderLines {
  readonly order = input.required<PurchaseOrder>();
  readonly disabled = input(false);
  readonly updated = output<{ id: string; change: OrderLineChange }>();
  readonly removed = output<string>();
  readonly editing = signal('');
  readonly removing = signal('');
  constructor() {
    effect(() => {
      this.order();
      this.editing.set('');
      this.removing.set('');
    });
  }
}
