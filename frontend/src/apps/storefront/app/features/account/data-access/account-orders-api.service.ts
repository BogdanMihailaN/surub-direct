import { inject, Injectable } from '@angular/core';
import { ApiClient } from '@shared/http/api-client.service';
import { DemoOrderPage } from '../../cart/models/demo-order-page.model';
@Injectable({ providedIn: 'root' })
export class AccountOrdersApi {
  private readonly api = inject(ApiClient);
  read(accountId: string, page: number) {
    return this.api.get<DemoOrderPage>(
      `/api/v1/ordering/account/orders?page=${page}&expectedAccountId=${encodeURIComponent(accountId)}`,
    );
  }
  browser(page: number) {
    return this.api.get<DemoOrderPage>(`/api/v1/ordering/demo-orders?page=${page}`);
  }
  claim(id: string, accountId: string) {
    return this.api.write<void>(
      'POST',
      `/api/v1/ordering/account/orders/${encodeURIComponent(id)}/claim?expectedAccountId=${encodeURIComponent(accountId)}`,
    );
  }
}
