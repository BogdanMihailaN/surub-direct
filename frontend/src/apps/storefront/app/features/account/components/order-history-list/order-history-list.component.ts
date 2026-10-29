import { Component, input, output } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { DemoOrder } from '../../../cart/models/demo-order.model';
import { DemoOrderPage } from '../../../cart/models/demo-order-page.model';
@Component({
  selector: 'sd-order-history-list',
  imports: [DatePipe, DecimalPipe],
  templateUrl: './order-history-list.component.html',
  styleUrl: './order-history-list.component.scss',
})
export class OrderHistoryList {
  readonly title = input.required<string>();
  readonly result = input<DemoOrderPage | null>(null);
  readonly error = input('');
  readonly busy = input(false);
  readonly allowClaim = input(false);
  readonly pageChange = output<number>();
  readonly claim = output<DemoOrder>();
  readonly labels = {
    Confirming: 'Confirmare în curs',
    Confirmed: 'Confirmată',
    Cancelling: 'Anulare în curs',
    Cancelled: 'Anulată',
    Failed: 'Confirmare nereușită',
  };
}
