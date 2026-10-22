import { inject, Injectable, signal } from '@angular/core';
import { errorMessage } from '@shared/http/error-message';
import { SalesOrdersApi } from '../data-access/sales-orders-api.service';
import { OrderPage } from '../models/order-page.model';
import { OrderDetail } from '../models/order-detail.model';
@Injectable()
export class SalesOrdersStore {
  private readonly api = inject(SalesOrdersApi);
  readonly cancelling = signal(false);
  readonly cancelError = signal('');
  readonly data = signal<OrderPage | null>(null);
  readonly detail = signal<OrderDetail | null>(null);
  readonly busy = signal(false);
  readonly loadingDetail = signal(false);
  readonly error = signal('');
  readonly detailError = signal('');
  readonly selected = signal<string | null>(null);
  private status = '';
  private number = '';
  private listSequence = 0;
  private detailSequence = 0;
  filter(status: string, number: string) {
    this.status = status;
    this.number = number;
    return this.load(1);
  }
  async load(page = this.data()?.page ?? 1) {
    const sequence = ++this.listSequence;
    this.busy.set(true);
    this.error.set('');
    try {
      const result = await this.api.list(this.status, this.number, page);
      if (sequence === this.listSequence) this.data.set(result);
    } catch (e) {
      if (sequence === this.listSequence) {
        this.data.set(null);
        this.error.set(errorMessage(e));
      }
    } finally {
      if (sequence === this.listSequence) this.busy.set(false);
    }
  }
  async cancel(reason: string) {
    const id = this.selected();
    if (!id || this.cancelling()) return;
    this.cancelling.set(true);
    this.cancelError.set('');
    try {
      await this.api.cancel(id, reason);
    } catch (e) {
      this.cancelError.set(errorMessage(e) + ' Actualizează starea înainte de a reîncerca.');
    } finally {
      if (this.selected() === id) await this.select(id);
      await this.load();
      this.cancelling.set(false);
    }
  }
  async select(id: string) {
    const sequence = ++this.detailSequence;
    this.selected.set(id);
    this.detail.set(null);
    this.detailError.set('');
    this.loadingDetail.set(true);
    try {
      const result = await this.api.detail(id);
      if (sequence === this.detailSequence) this.detail.set(result);
    } catch (e) {
      if (sequence === this.detailSequence) this.detailError.set(errorMessage(e));
    } finally {
      if (sequence === this.detailSequence) this.loadingDetail.set(false);
    }
  }
}
