import { inject, Injectable, signal } from '@angular/core';
import { PreparationApi } from '../data-access/preparation-api.service';
import { PreparedCart } from '../models/prepared-cart.model';
@Injectable()
export class PreparationStore {
  private readonly api = inject(PreparationApi);
  readonly enabled = signal(false);
  readonly snapshot = signal<PreparedCart | null>(null);
  readonly error = signal('');
  private sequence = 0;
  async configure() {
    try {
      this.enabled.set((await this.api.capabilities()).preparationEnabled);
    } catch {
      this.error.set('Pregătirea nu este disponibilă momentan.');
    }
  }
  async load(id: string | null) {
    const sequence = ++this.sequence;
    this.error.set('');
    if (!id) {
      this.snapshot.set(null);
      return;
    }
    try {
      const value = await this.api.read();
      if (sequence === this.sequence) {
        if (value?.id !== id) {
          this.snapshot.set(null);
          this.error.set('Starea pregătirii s-a schimbat. Reîncarcă coșul.');
        } else this.snapshot.set(value);
      }
    } catch {
      if (sequence === this.sequence) {
        this.snapshot.set(null);
        this.error.set('Nu putem confirma starea rezervării. Reîncearcă actualizarea.');
      }
    }
  }
}
