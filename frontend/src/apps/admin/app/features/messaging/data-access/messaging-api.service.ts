import { inject, Injectable } from '@angular/core';
import { ApiClient } from '@shared/http/api-client.service';
import { MessagingStatus } from '../models/messaging-status.model';
import { BrokerStatus } from '../models/broker-status.model';
import { OutboxStatus } from '../models/outbox-status.model';
@Injectable({ providedIn: 'root' })
export class MessagingApi {
  private readonly api = inject(ApiClient);
  read(page: number) {
    return this.api.get<MessagingStatus>('/api/v1/admin/messaging?page=' + page);
  }
  broker() {
    return this.api.get<BrokerStatus>('/api/v1/admin/messaging/broker');
  }
  outbox() {
    return this.api.get<OutboxStatus>('/api/v1/admin/messaging/inventory-outbox');
  }
  replay(id: string, requestId: string, reason: string) {
    return this.api.write('POST', '/api/v1/admin/messaging/' + encodeURIComponent(id) + '/replay', {
      requestId,
      reason,
    });
  }
}
