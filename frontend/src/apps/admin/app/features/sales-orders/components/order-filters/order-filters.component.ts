import { paymentLabels } from '../../models/payment-labels';
import { returnLabels } from '../../models/return-labels';
import { Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
@Component({
  selector: 'sd-sales-order-filters',
  imports: [FormsModule],
  templateUrl: './order-filters.component.html',
  styleUrl: './order-filters.component.scss',
})
export class OrderFilters {
  readonly busy = input(false);
  readonly applied = output<{ status: string; number: string; returnStatus: string; paymentStatus: string }>();
  readonly returnOptions = Object.entries(returnLabels);
  readonly paymentOptions = Object.entries(paymentLabels);
  paymentStatus = '';
  returnStatus = '';
  status = '';
  number = '';
}
