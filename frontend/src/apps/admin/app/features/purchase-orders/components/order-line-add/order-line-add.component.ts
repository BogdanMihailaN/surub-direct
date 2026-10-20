import { Component, input, output, computed } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { DecimalPipe } from '@angular/common';
import { Supplier } from '../../../purchasing/models/supplier.model';
import { PurchaseOrder } from '../../models/purchase-order.model';
@Component({
  selector: 'sd-order-line-add',
  imports: [FormsModule, DecimalPipe],
  templateUrl: './order-line-add.component.html',
  styleUrl: './order-line-add.component.scss',
})
export class OrderLineAdd {
  readonly supplier = input.required<Supplier>();
  readonly order = input.required<PurchaseOrder>();
  readonly disabled = input(false);
  readonly added = output<{ supplierItemId: string; packs: number }>();
  itemId = '';
  packs = 1;
  readonly items = computed(() =>
    this.supplier().items.filter(
      (i) =>
        i.isActive &&
        i.currency === this.order().currency &&
        !this.order().lines.some((l) => l.supplierItemId === i.id),
    ),
  );
  submit(form: NgForm) {
    if (
      form.valid &&
      !this.disabled() &&
      Number.isInteger(this.packs) &&
      this.items().some((i) => i.id === this.itemId)
    ) {
      this.added.emit({ supplierItemId: this.itemId, packs: this.packs });
      this.itemId = '';
      this.packs = 1;
    }
  }
}
