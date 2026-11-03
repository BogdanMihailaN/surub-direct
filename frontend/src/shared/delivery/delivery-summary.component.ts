import { Component, input } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { DeliveryCharge } from './delivery-charge.model';
@Component({
  selector: 'sd-delivery-summary',
  imports: [DecimalPipe],
  templateUrl: './delivery-summary.component.html',
  styleUrl: './delivery-summary.component.scss',
})
export class DeliverySummary {
  readonly delivery = input<DeliveryCharge | null | undefined>(null);
}
