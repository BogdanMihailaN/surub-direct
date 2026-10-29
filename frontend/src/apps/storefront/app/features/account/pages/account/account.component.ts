import { afterNextRender, Component, inject, signal } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { AccountStore } from '../../state/account.store';
import { AccountOrders } from '../../components/account-orders/account-orders.component';
import { AccountForm } from '../../components/account-form/account-form.component';
@Component({
  selector: 'sd-account',
  imports: [AccountForm, AccountOrders],
  providers: [AccountStore],
  templateUrl: './account.component.html',
  styleUrl: './account.component.scss',
  host: { '(window:focus)': 'store.load()' },
})
export class AccountPage {
  readonly store = inject(AccountStore);
  readonly register = signal(false);
  constructor() {
    inject(Title).setTitle('Contul meu | Șurub Direct');
    afterNextRender(() => void this.store.load());
  }
  changeMode(register: boolean) {
    this.register.set(register);
    this.store.error.set('');
    this.store.notice.set('');
  }
}
