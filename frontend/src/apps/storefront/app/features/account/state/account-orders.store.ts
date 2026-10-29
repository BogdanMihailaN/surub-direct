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
  readonly selected = signal<DemoOrder | null>(null);
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
      this.selected.set(order);
      this.error.set('');
      this.notice.set('');
    }
  }
  async confirm(accountId: string) {
    const order = this.selected();
    if (!order || this.busy() || !accountId) return;
    this.busy.set(true);
    this.selected.set(null);
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
