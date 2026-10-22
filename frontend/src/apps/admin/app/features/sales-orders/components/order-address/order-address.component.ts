import { Component, input } from '@angular/core';
import { OrderAddress as Address } from '../../models/order-address.model';
@Component({
  selector: 'sd-order-address',
  templateUrl: './order-address.component.html',
  styleUrl: './order-address.component.scss',
})
export class OrderAddress {
  readonly label = input.required<string>();
  readonly address = input.required<Address>();
}
