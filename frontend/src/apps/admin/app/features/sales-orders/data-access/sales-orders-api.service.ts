import { inject, Injectable } from '@angular/core';
import { ApiClient } from '@shared/http/api-client.service';
import { OrderPage } from '../models/order-page.model';
import { OrderDetail } from '../models/order-detail.model';
@Injectable({ providedIn: 'root' })
export class SalesOrdersApi {
  private readonly api = inject(ApiClient);
  private readonly root = '/api/v1/admin/sales-orders';
  list(status: string, number: string, page: number) {
    const params = new URLSearchParams({ status, number, page: String(page) });
    return this.api.get<OrderPage>(this.root + '?' + params);
  }
  detail(id: string) {
    return this.api.get<OrderDetail>(this.root + '/' + encodeURIComponent(id));
  }
}
