import { Component, input, output, effect } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { StockLedger } from '../../models/stock-ledger.model';
@Component({
  selector: 'sd-adjustment-form',
  imports: [FormsModule],
  templateUrl: './adjustment-form.component.html',
  styleUrl: './adjustment-form.component.scss',
})
export class AdjustmentForm {
  readonly ledger = input.required<StockLedger>();
  readonly disabled = input(false);
  readonly submitted = output<{ quantity: number; reference: string; reason: string }>();
  quantity: number | null = null;
  reference = '';
  reason = '';
  confirmed = false;
  constructor() {
    effect(() => {
      this.ledger();
      this.quantity = null;
      this.reference = '';
      this.reason = '';
      this.confirmed = false;
    });
  }
  submit(form: NgForm) {
    if (
      form.invalid ||
      this.disabled() ||
      !this.confirmed ||
      !this.quantity ||
      !Number.isInteger(this.quantity) ||
      !this.reference.trim() ||
      !this.reason.trim()
    )
      return;
    this.submitted.emit({
      quantity: this.quantity,
      reference: this.reference,
      reason: this.reason,
    });
  }
}
