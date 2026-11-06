import { inject, Injectable } from '@angular/core';
import { ApiClient } from '@shared/http/api-client.service';
import { AccountPartialReturns } from '../models/account-partial-returns.model';
import { AccountPartialSelection } from '../models/account-partial-selection.model';
@Injectable({ providedIn: 'root' })
export class AccountPartialReturnsApi {
  private readonly api = inject(ApiClient);
  private path(id: string) { return '/api/v1/ordering/account/orders/' + encodeURIComponent(id) + '/partial-returns'; }
  read(id: string, accountId: string) { return this.api.get<AccountPartialReturns>(this.path(id) + '?expectedAccountId=' + encodeURIComponent(accountId)); }
  request(id: string, accountId: string, requestId: string, selection: AccountPartialSelection) {
    return this.api.write('POST', this.path(id), { ...selection, expectedAccountId: accountId, requestId });
  }
}
