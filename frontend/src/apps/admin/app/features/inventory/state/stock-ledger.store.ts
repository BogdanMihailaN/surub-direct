import { inject, Injectable, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { errorMessage } from '@shared/http/error-message';
import { InventoryApi } from '../data-access/inventory-api.service';
import { StockLedger } from '../models/stock-ledger.model';
import { AdjustStockRequest } from '../models/adjust-stock.request';
@Injectable()
export class StockLedgerStore {
  private readonly api = inject(InventoryApi);
  readonly variant = signal('');
  readonly ledger = signal<StockLedger | null>(null);
  readonly busy = signal(false);
  readonly error = signal('');
  readonly message = signal('');
  readonly pending = signal<AdjustStockRequest | null>(null);
  async select(id: string) {
    if (this.busy() || this.pending()) return;
    this.variant.set(id);
    this.ledger.set(null);
    if (id) await this.load();
  }
  async load(page = 1) {
    if (this.busy()) return;
    this.busy.set(true);
    this.error.set('');
    this.message.set('');
    try {
      this.ledger.set(await this.api.read(this.variant(), page));
    } catch (e) {
      this.ledger.set(null);
      this.error.set(errorMessage(e));
    } finally {
      this.busy.set(false);
    }
  }
  async adjust(value: { quantity: number; reference: string; reason: string }) {
    const l = this.ledger();
    if (!l || this.pending() || this.busy()) return false;
    this.pending.set({
      ...value,
      requestId: crypto.randomUUID(),
      expectedOnHand: l.onHand,
      expectedReserved: l.reserved,
    });
    return this.retry();
  }
  async retry() {
    const c = this.pending();
    if (!c || this.busy()) return false;
    this.busy.set(true);
    this.error.set('');
    this.message.set('');
    try {
      await this.api.adjust(this.variant(), c);
      this.pending.set(null);
      this.ledger.set(await this.api.read(this.variant(), 1));
      this.message.set('Ajustarea a fost înregistrată.');
      return true;
    } catch (e) {
      if (e instanceof HttpErrorResponse && (e.status === 400 || e.status === 409))
        this.pending.set(null);
      this.ledger.set(null);
      this.error.set(errorMessage(e));
      return false;
    } finally {
      this.busy.set(false);
    }
  }
}
