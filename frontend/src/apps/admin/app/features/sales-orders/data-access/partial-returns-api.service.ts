import { inject, Injectable } from '@angular/core';
import { ApiClient } from '@shared/http/api-client.service';
import { PartialReturn } from '../models/partial-return.model';
import { PartialReturnSelection } from '../models/partial-return-selection.model';
import { PartialReturnAction } from '../models/partial-return-action.model';
@Injectable({ providedIn: 'root' })
export class PartialReturnsApi {
  private readonly api = inject(ApiClient);
  private path(orderId: string) { return '/api/v1/admin/sales-orders/' + encodeURIComponent(orderId) + '/partial-returns'; }
  list(orderId: string) { return this.api.get<PartialReturn[]>(this.path(orderId)); }
  create(orderId: string, selection: PartialReturnSelection) {
    return this.api.write<PartialReturn>('POST', this.path(orderId), { ...selection, requestId: crypto.randomUUID() });
  }
  act(orderId: string, action: PartialReturnAction) {
    const operation = action.target === 'RetryReceipt' ? 'retry-receipt' : action.target === 'Refund' ? 'refund' : action.target === 'Receive' ? 'receive' : 'decision';
    return this.api.write('POST', this.path(orderId) + '/' + encodeURIComponent(action.id) + '/' + operation,
      { operationId: crypto.randomUUID(), expectedVersion: action.version, target: action.target,
        reason: action.reason, resalableConfirmed: action.resalableConfirmed });
  }
}
