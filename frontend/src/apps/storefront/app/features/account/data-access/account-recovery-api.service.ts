import { inject, Injectable } from '@angular/core';
import { ApiClient } from '@shared/http/api-client.service';
import { RecoveryLink } from '../models/recovery-link';
@Injectable({ providedIn: 'root' })
export class AccountRecoveryApi {
  private readonly api = inject(ApiClient);
  request(kind: 'email' | 'password', email: string) {
    return this.api.write<void>(
      'POST',
      '/api/auth/' + (kind === 'email' ? 'request-email-confirmation' : 'request-password-reset'),
      { email },
    );
  }
  confirm(link: RecoveryLink) {
    return this.api.write<void>('POST', '/api/auth/confirm-email', link);
  }
  reset(link: RecoveryLink, password: string) {
    return this.api.write<void>('POST', '/api/auth/reset-password', { ...link, password });
  }
}
