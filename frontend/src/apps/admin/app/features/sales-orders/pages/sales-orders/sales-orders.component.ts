import { Component, inject, afterNextRender } from '@angular/core';
import { SalesOrdersStore } from '../../state/sales-orders.store';
import { OrderFilters } from '../../components/order-filters/order-filters.component';
import { OrderList } from '../../components/order-list/order-list.component';
import { OrderDetail } from '../../components/order-detail/order-detail.component';
@Component({
  selector: 'sd-sales-orders',
  imports: [OrderFilters, OrderList, OrderDetail],
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
