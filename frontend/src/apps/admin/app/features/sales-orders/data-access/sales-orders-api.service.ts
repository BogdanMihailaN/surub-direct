import { inject, Injectable } from '@angular/core';
import { ApiClient } from '@shared/http/api-client.service';
import { OrderPage } from '../models/order-page.model';
import { OrderDetail } from '../models/order-detail.model';
@Injectable({ providedIn: 'root' })
export class SalesOrdersApi {
  private readonly api = inject(ApiClient);
  private readonly root = '/api/v1/admin/sales-orders';
  list(status: string, number: string, page: number, returnStatus = '', paymentStatus = '', partialReturnStatus = '') {
    const params = new URLSearchParams({ status, number, returnStatus, paymentStatus, partialReturnStatus, page: String(page) });
    return this.api.get<OrderPage>(this.root + '?' + params);
  }
  cancel(id: string, reason: string) {
    return this.api.write('POST', this.root + '/' + encodeURIComponent(id) + '/cancel', { reason });
  }
  changeReturn(id: string, expectedVersion: number, target: string, reason: string, operationId: string, restockConfirmed: boolean) {
    return this.api.write('POST', this.root + '/' + encodeURIComponent(id) + '/return', { expectedVersion, target, reason, operationId, restockConfirmed });
  }
  changeShipment(id: string, expectedVersion: number, target: string, reason: string, operationId: string) {
    return this.api.write('POST', this.root + '/' + encodeURIComponent(id) + '/shipment', { expectedVersion, target, reason, operationId });
  }
  changeProcessing(id: string, expectedVersion: number, target: string, reason: string, operationId: string) {
    return this.api.write('POST', this.root + '/' + encodeURIComponent(id) + '/processing', { expectedVersion, target, reason, operationId });
  }
  simulatePayment(id: string, expectedVersion: number, action: string, reason: string, operationId: string) {
    return this.api.write('POST', this.root + '/' + encodeURIComponent(id) + '/payment-simulation', { expectedVersion, action, reason, operationId });
  }
  detail(id: string) {
    return this.api.get<OrderDetail>(this.root + '/' + encodeURIComponent(id));
  }
}
