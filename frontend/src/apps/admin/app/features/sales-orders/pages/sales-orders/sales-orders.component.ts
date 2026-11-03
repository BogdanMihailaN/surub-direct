import { OrderReturnComponent } from '../../components/order-return/order-return.component';
import { ShipmentComponent } from '../../components/shipment/shipment.component';
import { PaymentSimulation } from '../../components/payment-simulation/payment-simulation.component';
import { OrderProcessing } from '../../components/order-processing/order-processing.component';
import { OrderCancellation } from '../../components/order-cancellation/order-cancellation.component';
import { Component, inject, afterNextRender } from '@angular/core';
import { SalesOrdersStore } from '../../state/sales-orders.store';
import { OrderFilters } from '../../components/order-filters/order-filters.component';
import { OrderList } from '../../components/order-list/order-list.component';
import { OrderDetail } from '../../components/order-detail/order-detail.component';
@Component({
  selector: 'sd-sales-orders',
  imports: [OrderReturnComponent,ShipmentComponent,OrderFilters, OrderList, OrderDetail, OrderCancellation, OrderProcessing, PaymentSimulation],
  providers: [SalesOrdersStore],
  templateUrl: './sales-orders.component.html',
  styleUrl: './sales-orders.component.scss',
})
export class SalesOrdersPage {
  readonly state = inject(SalesOrdersStore);
  constructor() {
    afterNextRender(() => void this.state.load());
  }
}
