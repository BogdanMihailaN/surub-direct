import { Component, input, output, effect } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { StoreProduct } from '@shared/models';
import { ReceiveStockRequest } from '../../models/receive-stock.request';
@Component({
  selector: 'sd-receipt-form',
  imports: [FormsModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  templateUrl: './receipt-form.component.html',
  styleUrl: './receipt-form.component.scss',
})
export class ReceiptForm {
  readonly busy = input(false);
  readonly item = input.required<StoreProduct>();
  readonly submitted = output<ReceiveStockRequest>();
  receiptVariant = '';
  quantity: number | null = null;
  reference = '';
  receiptRequestId: string | null = null;
  private stockStamp = '';
  constructor() {
    effect(() => {
      const stamp = JSON.stringify(this.item().stock);
      if (stamp !== this.stockStamp) {
        this.stockStamp = stamp;
        this.quantity = null;
        this.reference = '';
        this.receiptRequestId = null;
      }
    });
  }
  submit(form: NgForm) {
    if (form.invalid || this.busy()) return;
    if (this.quantity === null) return;
    this.receiptRequestId ??= crypto.randomUUID();
    this.submitted.emit({
      variantId: this.receiptVariant,
      requestId: this.receiptRequestId,
      quantity: this.quantity,
      reference: this.reference,
    });
  }
}
