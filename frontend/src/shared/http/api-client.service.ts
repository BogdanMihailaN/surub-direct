import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { API_ORIGIN } from './api-origin.token';
@Injectable({ providedIn: 'root' })
export class ApiClient {
  private readonly http = inject(HttpClient);
  private readonly origin = inject(API_ORIGIN);
  get<T>(path: string) {
    return firstValueFrom(this.http.get<T>(this.origin + path));
  }
  async write<T>(method: string, path: string, body: unknown = {}) {
    const csrf = await this.get<{ token: string }>('/api/auth/csrf');
    return firstValueFrom(
      this.http.request<T>(method, this.origin + path, {
        body,
        headers: { 'X-CSRF-TOKEN': csrf.token },
      }),
    );
  }
}
