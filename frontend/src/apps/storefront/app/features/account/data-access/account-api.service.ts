import { inject, Injectable } from '@angular/core';
import { ApiClient } from '@shared/http/api-client.service';
import { AccountSession } from '../models/account-session';
@Injectable({ providedIn: 'root' })
export class AccountApi {
  private readonly api = inject(ApiClient);
  session() {
    return this.api.get<AccountSession>('/api/auth/me');
  }
  login(email: string, password: string) {
    return this.api.write<void>('POST', '/api/auth/login', { email, password });
  }
  register(email: string, password: string) {
    return this.api.write<void>('POST', '/api/auth/register', { email, password });
  }
  logout() {
    return this.api.write<void>('POST', '/api/auth/logout');
  }
}
