import { inject, Injectable, signal } from '@angular/core';
import { errorMessage } from '@shared/http/error-message';
import { AccountOrdersApi } from '../data-access/account-orders-api.service';
import { DemoOrder } from '../../cart/models/demo-order.model';
import { DemoOrderPage } from '../../cart/models/demo-order-page.model';
@Injectable()
export class AccountOrdersStore {
  private readonly api = inject(AccountOrdersApi);
  readonly account = signal<DemoOrderPage | null>(null);
  readonly browser = signal<DemoOrderPage | null>(null);
  readonly accountError = signal('');
  readonly browserError = signal('');
  readonly error = signal('');
  readonly notice = signal('');
  readonly busy = signal(false);
  readonly partialSelection = signal<DemoOrder | null>(null);
  readonly selected = signal<DemoOrder | null>(null);
  readonly returnSelection = signal<{ order: DemoOrder; accountId: string } | null>(null);
  private accountPage = 1;
  private browserPage = 1;
  private async refresh(accountId: string) {
    this.account.set(null);
    this.browser.set(null);
    this.accountError.set('');
    this.browserError.set('');
    const [account, browser] = await Promise.allSettled([
      this.api.read(accountId, this.accountPage),
      this.api.browser(this.browserPage),
    ]);
    if (account.status === 'fulfilled') this.account.set(account.value);
    else this.accountError.set(errorMessage(account.reason));
    if (browser.status === 'fulfilled') this.browser.set(browser.value);
    else this.browserError.set(errorMessage(browser.reason));
  }
  async load(accountId: string, kind?: 'account' | 'browser', page = 1) {
    if (this.busy()) return;
    this.selected.set(null);
    this.partialSelection.set(null);
    this.returnSelection.set(null);
    this.error.set('');
    this.notice.set('');
    if (!accountId) {
      this.accountError.set('Sesiunea trebuie reverificată. Reîncarcă pagina.');
      return;
    }
    if (kind === 'account') this.accountPage = page;
    if (kind === 'browser') this.browserPage = page;
    this.busy.set(true);
    try {
      await this.refresh(accountId);
    } finally {
      this.busy.set(false);
    }
  }
  select(order: DemoOrder) {
    if (!this.busy() && this.account() && ['Confirmed', 'Cancelled'].includes(order.status)) {
      this.returnSelection.set(null);
      this.partialSelection.set(null);
      this.selected.set(order);
      this.error.set('');
      this.notice.set('');
    }
  }
  selectPartial(order: DemoOrder) {
    if (this.busy() || !this.account()?.items.some(x => x.id === order.id) || order.status !== 'Confirmed' || order.shipment?.status !== 'Delivered') return;
    this.selected.set(null); this.returnSelection.set(null); this.partialSelection.set(order);
  }
  selectReturn(order: DemoOrder, accountId: string) {
    if (this.busy() || !accountId || !this.account()?.items.some(item => item.id === order.id)
        || order.status !== 'Confirmed' || order.shipment?.status !== 'Delivered' || order.return?.status !== 'NotRequested') return;
    this.selected.set(null);
    this.partialSelection.set(null);
    this.error.set(''); this.notice.set('');
    this.returnSelection.set({ order, accountId });
  }
  async requestReturn(accountId: string, reason: string) {
    const selection = this.returnSelection();
    if (!selection || this.busy() || reason.trim().length < 3 || reason.trim().length > 500) return;
    this.returnSelection.set(null);
    if (selection.accountId !== accountId) {
      this.error.set('Contul s-a schimbat. Reîncarcă istoricul și confirmă din nou solicitarea.');
      return;
    }
    this.busy.set(true); this.error.set(''); this.notice.set('');
    try {
      try {
        await this.api.requestReturn(selection.order.id, selection.accountId, crypto.randomUUID(), reason.trim());
        this.notice.set('Solicitarea de retur a fost trimisă pentru verificare.');
      } catch (e) {
        this.error.set(errorMessage(e) + ' Istoricul este recitit; solicitarea nu este retrimisă automat.');
      }
      await this.refresh(selection.accountId);
      const status = this.account()?.items.find(item => item.id === selection.order.id)?.return?.status;
      if (this.error() && status && status !== 'NotRequested') {
        this.error.set('');
        this.notice.set('Starea returului a fost reverificată. Consultă rezultatul din istoric.');
      }
    } finally { this.busy.set(false); }
  }
  async confirm(accountId: string) {
    const order = this.selected();
    if (!order || this.busy() || !accountId) return;
    this.busy.set(true);
    this.selected.set(null);
    this.partialSelection.set(null);
    this.returnSelection.set(null);
    this.error.set('');
    this.notice.set('');
    try {
      try {
        await this.api.claim(order.id, accountId);
        this.notice.set(`Comanda ${order.number} a fost asociată contului.`);
      } catch (e) {
        this.error.set(
          `${errorMessage(e)} Listele sunt reverificate; dacă răspunsul s-a pierdut, comanda poate fi deja asociată.`,
        );
      }
      this.accountPage = 1;
      this.browserPage = 1;
      await this.refresh(accountId);
      if (this.error() && this.account()?.items.some((item) => item.id === order.id)) {
        this.error.set('');
        this.notice.set(
          `Comanda ${order.number} este asociată contului. Starea a fost reverificată.`,
        );
      }
    } finally {
      this.busy.set(false);
    }
  }
}
