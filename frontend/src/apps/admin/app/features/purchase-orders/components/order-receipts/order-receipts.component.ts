import { Component, input, output, effect } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { PurchaseOrder } from '../../models/purchase-order.model';
import { ReceivePurchaseOrderRequest } from '../../models/receive-purchase-order.request';
@Component({
  selector: 'sd-order-receipts',
  imports: [FormsModule, DatePipe],
  templateUrl: './order-receipts.component.html',
  styleUrl: './order-receipts.component.scss',
})
export class OrderReceipts {
  readonly order = input.required<PurchaseOrder>();
  readonly disabled = input(false);
  readonly received = output<ReceivePurchaseOrderRequest>();
  readonly retried = output<string>();
  lineId = '';
  quantity: number | null = null;
  reference = '';
  requestId = crypto.randomUUID();
  constructor() {
    effect(() => {
      this.order();
      this.lineId = '';
      this.quantity = null;
      this.reference = '';
      this.requestId = crypto.randomUUID();
    });
  }
  confirmed(id: string) {
    return (
      this.order()
        .receipts.filter((r) => r.lineId === id && r.appliedAt)
        .reduce((n, r) => n + r.quantity, 0) -
      this.order()
        .corrections.filter((c) => c.lineId === id && c.appliedAt)
        .reduce((n, c) => n + c.quantity, 0)
    );
  }
  pending(id: string) {
    return this.order()
      .receipts.filter((r) => r.lineId === id && !r.appliedAt)
      .reduce((n, r) => n + r.quantity, 0);
  }
  remaining(id: string) {
    const line = this.order().lines.find((l) => l.id === id);
    return line ? line.baseUnits - this.confirmed(id) - this.pending(id) : 0;
  }
  sku(id: string) {
    return this.order().lines.find((l) => l.id === id)?.sku ?? '';
  }
  hasRemaining() {
    return this.order().lines.some((line) => this.remaining(line.id) > 0);
  }
  hasPendingCorrections() {
    return this.order().corrections.some((c) => !c.appliedAt);
  }
  hasPending() {
    return this.order().receipts.some((receipt) => !receipt.appliedAt);
  }
  limit() {
    return Math.min(10000000, this.remaining(this.lineId));
  }
  submit(form: NgForm) {
    if (
      form.invalid ||
      this.disabled() ||
      !Number.isInteger(this.quantity) ||
      !this.quantity ||
      this.quantity > this.limit() ||
      !this.reference.trim()
    )
      return;
    this.received.emit({
      requestId: this.requestId,
      lineId: this.lineId,
      quantity: this.quantity,
      reference: this.reference,
    });
  }
}
