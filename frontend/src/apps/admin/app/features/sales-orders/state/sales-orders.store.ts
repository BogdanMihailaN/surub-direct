import { PartialReturnsApi } from '../data-access/partial-returns-api.service';
import { PartialReturn } from '../models/partial-return.model';
import { PartialReturnSelection } from '../models/partial-return-selection.model';
import { PartialReturnAction } from '../models/partial-return-action.model';
import { inject, Injectable, signal } from '@angular/core';
import { errorMessage } from '@shared/http/error-message';
import { SalesOrdersApi } from '../data-access/sales-orders-api.service';
import { OrderPage } from '../models/order-page.model';
import { OrderDetail } from '../models/order-detail.model';
@Injectable()
export class SalesOrdersStore {
  private readonly partialApi = inject(PartialReturnsApi);
  readonly partialReturns = signal<PartialReturn[]>([]);
  readonly partialBusy = signal(false);
  readonly partialError = signal('');
  hasActivePartial() { return this.partialReturns().some(x => x.status !== 'Rejected'); }
  private readonly api = inject(SalesOrdersApi);
  readonly cancelling = signal(false);
  readonly returning = signal(false);
  readonly returnError = signal('');
  readonly shipping = signal(false);
  readonly shippingError = signal('');
  readonly processing = signal(false);
  readonly simulatingPayment = signal(false);
  readonly paymentError = signal('');
  readonly processingError = signal('');
  readonly cancelError = signal('');
  readonly data = signal<OrderPage | null>(null);
  readonly detail = signal<OrderDetail | null>(null);
  readonly busy = signal(false);
  readonly loadingDetail = signal(false);
  readonly error = signal('');
  readonly detailError = signal('');
  readonly selected = signal<string | null>(null);
  private partialReturnStatus = '';
  private paymentStatus = '';
  private returnStatus = '';
  private status = '';
  private number = '';
  private listSequence = 0;
  private detailSequence = 0;
  filter(status: string, number: string, returnStatus = '', paymentStatus = '', partialReturnStatus = '') {
    this.partialReturnStatus = partialReturnStatus;
    this.paymentStatus = paymentStatus;
    this.returnStatus = returnStatus;
    this.status = status;
    this.number = number;
    return this.load(1);
  }
  async load(page = this.data()?.page ?? 1) {
    const sequence = ++this.listSequence;
    this.busy.set(true);
    this.error.set('');
    try {
      let result = await this.api.list(this.status, this.number, page, this.returnStatus, this.paymentStatus, this.partialReturnStatus);
      if (sequence !== this.listSequence) return;
      // A decision in this or another session can remove the last row of a later page.
      // Re-read once from page one, keeping the filters and the same request sequence.
      if (result.page > 1 && result.items.length === 0) {
        result = await this.api.list(this.status, this.number, 1, this.returnStatus, this.paymentStatus, this.partialReturnStatus);
      }
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
    if (!id || this.cancelling() || this.processing() || this.simulatingPayment() || this.shipping() || this.partialBusy() || this.returning() || this.blocksCancellation()) return;
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
  async changeReturn(change: { target: string; reason: string; restockConfirmed: boolean }) {
    const detail = this.detail();
    if (!detail || this.processing() || this.cancelling() || this.simulatingPayment() || this.shipping() || this.partialBusy() || this.returning() || detail.order.status !== 'Confirmed') return;
    const id = detail.order.id;
    const sequence = this.detailSequence;
    this.returning.set(true);
    this.returnError.set('');
    try {
      await this.api.changeReturn(id, detail.return.version, change.target, change.reason, crypto.randomUUID(), change.restockConfirmed);
    } catch (e) {
      if (sequence === this.detailSequence) this.returnError.set(errorMessage(e) + ' Starea este recitită; operația nu este retrimisă automat.');
    } finally {
      if (this.selected() === id) await this.select(id);
      await this.load();
      this.returning.set(false);
    }
  }
  async changeShipment(change: { target: string; reason: string }) {
    const detail = this.detail();
    if (!detail || this.processing() || this.cancelling() || this.simulatingPayment() || this.shipping() || this.partialBusy() || this.returning() || detail.order.status !== 'Confirmed') return;
    const id = detail.order.id;
    const sequence = this.detailSequence;
    this.shipping.set(true);
    this.shippingError.set('');
    try {
      await this.api.changeShipment(id, detail.shipment.version, change.target, change.reason, crypto.randomUUID());
    } catch (e) {
      if (sequence === this.detailSequence) this.shippingError.set(errorMessage(e) + ' Starea este recitită; operația nu este retrimisă automat.');
    } finally {
      if (this.selected() === id) await this.select(id);
      this.shipping.set(false);
    }
  }
  async changeProcessing(change: { target: string; reason: string }) {
    const detail = this.detail();
    if (!detail || this.processing() || this.cancelling() || this.simulatingPayment() || this.shipping() || this.partialBusy() || this.returning() || detail.order.status !== 'Confirmed') return;
    const id = detail.order.id;
    const sequence = this.detailSequence;
    this.processing.set(true);
    this.processingError.set('');
    try {
      await this.api.changeProcessing(id, detail.processing.version, change.target, change.reason, crypto.randomUUID());
    } catch (e) {
      if (sequence === this.detailSequence) this.processingError.set(errorMessage(e) + ' Starea este recitită; operația nu este retrimisă automat.');
    } finally {
      if (this.selected() === id) await this.select(id);
      this.processing.set(false);
    }
  }
  blocksCancellation() {
    const status = this.detail()?.paymentSimulation.status;
    return status === 'Pending' || status === 'Paid' || status === 'PartiallyRefunded' || ['Shipped', 'Delivered'].includes(this.detail()?.shipment.status ?? '');
  }
  async simulatePayment(change: { action: string; reason: string }) {
    const detail = this.detail();
    if (!detail || this.simulatingPayment() || this.shipping() || this.partialBusy() || this.returning() || this.processing() || this.cancelling() || detail.order.status !== 'Confirmed') return;
    const id = detail.order.id;
    const sequence = this.detailSequence;
    this.simulatingPayment.set(true);
    this.paymentError.set('');
    try {
      await this.api.simulatePayment(id, detail.paymentSimulation.version, change.action, change.reason, crypto.randomUUID());
    } catch (e) {
      if (sequence === this.detailSequence) this.paymentError.set(errorMessage(e) + ' Starea este recitită; operația nu este repetată automat.');
    } finally {
      if (this.selected() === id) await this.select(id);
      await this.load();
      this.simulatingPayment.set(false);
    }
  }
  async createPartial(selection: PartialReturnSelection) {
    await this.partialMutation(id => this.partialApi.create(id, selection));
  }
  async actPartial(action: PartialReturnAction) {
    await this.partialMutation(id => this.partialApi.act(id, action));
  }
  private async partialMutation(write: (id: string) => Promise<unknown>) {
    const detail = this.detail();
    if (!detail || this.partialBusy() || this.cancelling() || this.processing() || this.shipping() || this.simulatingPayment() || this.returning()) return;
    const id = detail.order.id;
    const sequence = this.detailSequence;
    this.partialBusy.set(true); this.partialError.set('');
    try { await write(id); }
    catch (error) {
      if (sequence === this.detailSequence) this.partialError.set(errorMessage(error) + ' Starea este recitită; operația nu este retrimisă automat.');
    } finally {
      if (this.selected() === id) await this.select(id);
      await this.load();
      this.partialBusy.set(false);
    }
  }
  async select(id: string) {
    if (this.selected() !== id) this.partialError.set('');
    if (this.selected() !== id) this.returnError.set('');
    if (this.selected() !== id) this.shippingError.set('');
    if (this.selected() !== id) this.paymentError.set('');
    if (this.selected() !== id) this.processingError.set('');
    const sequence = ++this.detailSequence;
    this.selected.set(id);
    this.detail.set(null);
    this.partialReturns.set([]);
    this.detailError.set('');
    this.loadingDetail.set(true);
    try {
      const [result, returns] = await Promise.all([this.api.detail(id), this.partialApi.list(id)]);
      if (sequence === this.detailSequence) { this.detail.set(result); this.partialReturns.set(returns); }
    } catch (e) {
      if (sequence === this.detailSequence) this.detailError.set(errorMessage(e));
    } finally {
      if (sequence === this.detailSequence) this.loadingDetail.set(false);
    }
  }
}
