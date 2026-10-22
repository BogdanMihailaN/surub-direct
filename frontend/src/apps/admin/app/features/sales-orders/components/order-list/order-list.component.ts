import { Component, input, output } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { OrderPage } from '../../models/order-page.model';
import { orderLabels } from '../../models/order-labels';
@Component({
  selector: 'sd-sales-order-list',
  imports: [DatePipe, DecimalPipe],
  templateUrl: './order-list.component.html',
  styleUrl: './order-list.component.scss',
})
export class OrderList {
  readonly data = input.required<OrderPage>();
  readonly selected = input<string | null>(null);
  readonly busy = input(false);
  readonly chosen = output<string>();
  readonly paged = output<number>();
  readonly labels = orderLabels;
}
