import { Component, input } from '@angular/core';
import { DemoOrder } from '../../models/demo-order.model';
@Component({ selector: 'sd-shipment-summary', templateUrl: './shipment-summary.component.html', styleUrl: './shipment-summary.component.scss' })
export class ShipmentSummary {
  readonly data = input<DemoOrder['shipment']>();
  readonly labels: Record<string, string> = { NotStarted: 'Necreată', Ready: 'Pregătită pentru expediere', Shipped: 'Expediată', Delivered: 'Livrată' };
}
