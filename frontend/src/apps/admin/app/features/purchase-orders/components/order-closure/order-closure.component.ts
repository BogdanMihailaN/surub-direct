import { Component, input, output, effect } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { PurchaseOrder } from '../../models/purchase-order.model';
@Component({
  selector: 'sd-order-closure',
  imports: [FormsModule, DatePipe],
  templateUrl: './order-closure.component.html',
  styleUrl: './order-closure.component.scss',
})
export class OrderClosure {
  readonly order = input.required<PurchaseOrder>();
  readonly disabled = input(false);
  readonly closed = output<string>();
  reason = '';
  confirmed = false;
  constructor() {
    effect(() => {
      this.order();
      this.reason = '';
      this.confirmed = false;
    });
  }
  remaining(id: string) {
    const o = this.order();
    return (
      (o.lines.find((l) => l.id === id)?.baseUnits ?? 0) -
      o.receipts.filter((r) => r.lineId === id && r.appliedAt).reduce((n, r) => n + r.quantity, 0) +
      o.corrections
        .filter((c) => c.lineId === id && c.appliedAt)
        .reduce((n, c) => n + c.quantity, 0)
    );
  }
  totalRemaining() {
    return this.order().lines.reduce((n, l) => n + this.remaining(l.id), 0);
  }
  pending() {
    return (
      this.order().receipts.some((r) => !r.appliedAt) ||
      this.order().corrections.some((c) => !c.appliedAt)
    );
  }
  submit(form: NgForm) {
    if (
      form.invalid ||
      this.disabled() ||
      this.pending() ||
      !this.confirmed ||
      (this.totalRemaining() > 0 && !this.reason.trim())
    )
      return;
    this.closed.emit(this.reason);
  }
}
