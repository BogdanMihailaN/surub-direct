import { inject, Injectable, signal } from '@angular/core';
import { ApiClient } from '@shared/http/api-client.service';
@Injectable({ providedIn: 'root' })
export class SessionService {
  private readonly api = inject(ApiClient);
  readonly authenticated = signal(false);
  async check() {
    try {
      const me = await this.api.get<{ isAdmin: boolean }>('/api/auth/me');
      this.authenticated.set(me.isAdmin);
      return me.isAdmin;
    } catch {
      this.authenticated.set(false);
      return false;
    }
  }
  async login(email: string, password: string) {
    await this.api.write('POST', '/api/auth/login', { email, password });
    if (!(await this.check())) throw new Error('Acces administrativ necesar.');
  }
  async logout() {
    await this.api.write('POST', '/api/auth/logout');
    this.authenticated.set(false);
  }
}
