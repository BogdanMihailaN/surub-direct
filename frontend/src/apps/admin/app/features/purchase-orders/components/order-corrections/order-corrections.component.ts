import { Component, input, output, effect } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { PurchaseOrder } from '../../models/purchase-order.model';
import { CorrectPurchaseReceiptRequest } from '../../models/correct-purchase-receipt.request';
@Component({
  selector: 'sd-order-corrections',
  imports: [FormsModule, DatePipe],
  templateUrl: './order-corrections.component.html',
  styleUrl: './order-corrections.component.scss',
})
export class OrderCorrections {
  readonly order = input.required<PurchaseOrder>();
  readonly disabled = input(false);
  readonly corrected = output<CorrectPurchaseReceiptRequest>();
  readonly retried = output<string>();
  receiptId = '';
  quantity: number | null = null;
  reference = '';
  reason = '';
  confirmed = false;
  requestId = crypto.randomUUID();
  constructor() {
    effect(() => {
      this.order();
      this.receiptId = '';
      this.quantity = null;
      this.reference = '';
      this.reason = '';
      this.confirmed = false;
      this.requestId = crypto.randomUUID();
    });
  }
  available(id: string) {
    return (
      (this.order().receipts.find((r) => r.id === id)?.quantity ?? 0) -
      this.order()
        .corrections.filter((c) => c.receiptId === id)
        .reduce((n, c) => n + c.quantity, 0)
    );
  }
  receipts() {
    return this.order().receipts.filter((r) => r.appliedAt && this.available(r.id) > 0);
  }
  document(id: string) {
    return this.order().receipts.find((r) => r.id === id)?.reference ?? '';
  }
  sku(id: string) {
    return this.order().lines.find((l) => l.id === id)?.sku ?? '';
  }
  submit(form: NgForm) {
    if (
      form.invalid ||
      this.disabled() ||
      !this.confirmed ||
      !Number.isInteger(this.quantity) ||
      !this.quantity ||
      this.quantity < 1 ||
      this.quantity > this.available(this.receiptId) ||
      !this.reference.trim() ||
      !this.reason.trim()
    )
      return;
    this.corrected.emit({
      requestId: this.requestId,
      receiptId: this.receiptId,
      quantity: this.quantity,
      reference: this.reference,
      reason: this.reason,
    });
  }
}
