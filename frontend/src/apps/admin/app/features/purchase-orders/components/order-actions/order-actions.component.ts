import { Component, input, output, effect } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { PurchaseOrder } from '../../models/purchase-order.model';
@Component({
  selector: 'sd-order-actions',
  imports: [FormsModule, DatePipe],
  templateUrl: './order-actions.component.html',
  styleUrl: './order-actions.component.scss',
})
export class OrderActions {
  readonly order = input.required<PurchaseOrder>();
  readonly disabled = input(false);
  readonly sent = output<void>();
  readonly cancelled = output<string>();
  confirmSent = false;
  showCancel = false;
  reason = '';
  constructor() {
    effect(() => {
      this.order();
      this.confirmSent = false;
      this.showCancel = false;
      this.reason = '';
    });
  }
}
