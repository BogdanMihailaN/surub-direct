import { inject, Injectable, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ApiClient } from '@shared/http/api-client.service';
export type AdminSessionStatus = 'authenticated' | 'guest' | 'forbidden' | 'unavailable';

@Injectable({ providedIn: 'root' })
export class SessionService {
  private readonly api = inject(ApiClient);
  readonly authenticated = signal(false);
  async check(): Promise<AdminSessionStatus> {
    try {
      const me = await this.api.get<{ isAdmin: boolean }>('/api/auth/me');
      this.authenticated.set(me.isAdmin);
      return me.isAdmin ? 'authenticated' : 'forbidden';
    } catch (error) {
      this.authenticated.set(false);
      if (error instanceof HttpErrorResponse && error.status === 401) return 'guest';
      if (error instanceof HttpErrorResponse && error.status === 403) return 'forbidden';
      return 'unavailable';
    }
  }
  async login(email: string, password: string) {
    await this.api.write('POST', '/api/auth/login', { email, password });
    return this.check();
  }
  async logout() {
    await this.api.write('POST', '/api/auth/logout');
    this.authenticated.set(false);
  }
}
