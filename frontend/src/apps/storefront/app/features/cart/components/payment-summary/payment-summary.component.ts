import { Component, input } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { PaymentSummary } from '../../models/payment-summary.model';
@Component({
  selector: 'sd-payment-summary',
  imports: [DecimalPipe],
  templateUrl: './payment-summary.component.html',
  styleUrl: './payment-summary.component.scss',
})
export class PaymentSummaryComponent {
  readonly data = input<PaymentSummary>();
  readonly labels: Record<PaymentSummary['status'], string> = {
    NotStarted: 'Neinițiată', Pending: 'În curs', Paid: 'Reușită',
    Failed: 'Eșuată', Refunded: 'Rambursată integral',
  };
}
