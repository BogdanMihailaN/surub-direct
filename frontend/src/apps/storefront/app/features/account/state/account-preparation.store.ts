import { inject, Injectable, signal } from '@angular/core';
import { errorMessage } from '@shared/http/error-message';
import { PreparedCart } from '../../cart/models/prepared-cart.model';
import { AccountPreparationApi } from '../data-access/account-preparation-api.service';
@Injectable()
export class AccountPreparationStore {
  private readonly api = inject(AccountPreparationApi);
  private generation = 0;
  readonly snapshot = signal<PreparedCart | null>(null);
  readonly error = signal('');
  readonly enabled = signal(false);
  readonly loading = signal(false);
  clear() {
    this.generation++;
    this.snapshot.set(null);
    this.error.set('');
    this.loading.set(false);
  }
  async configure() {
    try {
      this.enabled.set((await this.api.capabilities()).preparationEnabled);
    } catch {
      this.enabled.set(false);
    }
  }
  async load(accountId: string, id: string) {
    const generation = ++this.generation;
    this.loading.set(true);
    this.error.set('');
    try {
      const value = await this.api.read(accountId);
      if (generation !== this.generation) return;
      if (value?.id !== id) {
        this.snapshot.set(null);
        this.error.set('Pregătirea s-a schimbat. Reîncarcă coșurile.');
      } else this.snapshot.set(value);
    } catch (error) {
      if (generation === this.generation) {
        this.snapshot.set(null);
        this.error.set('Nu putem confirma starea rezervării. ' + errorMessage(error));
      }
    } finally {
      if (generation === this.generation) this.loading.set(false);
    }
  }
}
