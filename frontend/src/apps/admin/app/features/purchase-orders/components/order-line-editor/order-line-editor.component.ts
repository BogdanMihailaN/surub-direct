import { Component, input, output, effect } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { PurchaseOrderLine } from '../../models/purchase-order-line.model';
import { OrderLineChange } from '../../models/order-line-change.request';
@Component({
  selector: 'sd-order-line-editor',
  imports: [FormsModule],
  templateUrl: './order-line-editor.component.html',
  styleUrl: './order-line-editor.component.scss',
})
export class OrderLineEditor {
  readonly line = input.required<PurchaseOrderLine>();
  readonly disabled = input(false);
  readonly saved = output<OrderLineChange>();
  readonly cancelled = output<void>();
  packs = 1;
  cost = 0;
  constructor() {
    effect(() => {
      this.packs = this.line().packs;
      this.cost = this.line().netCostPerPack;
    });
  }
  submit(form: NgForm) {
    if (form.valid && !this.disabled() && Number.isInteger(this.packs))
      this.saved.emit({ packs: this.packs, netCostPerPack: this.cost });
  }
}
