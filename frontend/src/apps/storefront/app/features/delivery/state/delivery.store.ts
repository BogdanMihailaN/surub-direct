import { Injectable, inject, signal, DestroyRef } from '@angular/core';
import { errorMessage } from '@shared/http/error-message';
import { DeliveryCharge } from '@shared/delivery/delivery-charge.model';
import { DeliveryApi } from '../data-access/delivery-api.service';
@Injectable()
export class DeliveryStore {
  private readonly api = inject(DeliveryApi);
  private generation = 0;
  readonly options = signal<DeliveryCharge[]>([]);
  readonly busy = signal(false);
  readonly error = signal('');
  constructor() { inject(DestroyRef).onDestroy(() => ++this.generation); }
  async load() {
    const generation = ++this.generation;
    this.busy.set(true);
    this.error.set('');
    try {
      const options = await this.api.options();
      if (generation === this.generation) this.options.set(options);
    } catch (e) {
      if (generation === this.generation) { this.options.set([]); this.error.set(errorMessage(e)); }
    } finally { if (generation === this.generation) this.busy.set(false); }
  }
}
