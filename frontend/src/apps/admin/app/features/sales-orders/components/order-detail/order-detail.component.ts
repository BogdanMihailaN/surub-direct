import { Component, input } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { OrderDetail as Detail } from '../../models/order-detail.model';
import { orderLabels } from '../../models/order-labels';
import { OrderAddress } from '../order-address/order-address.component';
@Component({
  selector: 'sd-sales-order-detail',
  imports: [DatePipe, DecimalPipe, OrderAddress],
  templateUrl: './order-detail.component.html',
  styleUrl: './order-detail.component.scss',
})
export class OrderDetail {
  readonly data = input.required<Detail>();
  readonly labels = orderLabels;
}
