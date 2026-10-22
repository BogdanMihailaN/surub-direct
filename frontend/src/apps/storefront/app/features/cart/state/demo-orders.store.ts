import { inject, Injectable, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { DemoOrdersApi } from '../data-access/demo-orders-api.service';
import { DemoOrderPage } from '../models/demo-order-page.model';
@Injectable()
export class DemoOrdersStore {
  private readonly api = inject(DemoOrdersApi);
  readonly result = signal<DemoOrderPage | null>(null);
  readonly error = signal('');
  readonly busy = signal(false);
  async load(page = 1) {
    if (this.busy()) return;
    this.busy.set(true);
    this.error.set('');
    try {
      this.result.set(await this.api.read(page));
    } catch (error) {
      this.result.set(null);
      if (!(error instanceof HttpErrorResponse && error.status === 404))
        this.error.set('Istoricul comenzilor demonstrative nu poate fi citit. Reîncearcă.');
    } finally {
      this.busy.set(false);
    }
  }
}
