import { Component, input, output, effect } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PurchaseOrder } from '../../models/purchase-order.model';
@Component({
  selector: 'sd-order-notes',
  imports: [FormsModule],
  templateUrl: './order-notes.component.html',
  styleUrl: './order-notes.component.scss',
})
export class OrderNotes {
  readonly order = input.required<PurchaseOrder>();
  readonly disabled = input(false);
  readonly saved = output<string>();
  notes = '';
  constructor() {
    effect(() => {
      this.notes = this.order().notes;
    });
  }
}
