import { afterNextRender, Component, inject, input, output } from '@angular/core';
import { DatePipe } from '@angular/common';
import { DemoOrder } from '../../../cart/models/demo-order.model';
import { AccountSession } from '../../models/account-session';
import { AccountPartialReturnsStore } from '../../state/account-partial-returns.store';
import { AccountPartialReturnForm } from '../partial-return-form/partial-return-form.component';
@Component({ selector: 'sd-account-partial-returns', imports: [DatePipe, AccountPartialReturnForm], providers: [AccountPartialReturnsStore], templateUrl: './account-partial-returns.component.html', styleUrl: './account-partial-returns.component.scss' })
export class AccountPartialReturnsComponent {
  readonly order = input.required<DemoOrder>();
  readonly session = input.required<AccountSession>();
  readonly closed = output<void>();
  readonly state = inject(AccountPartialReturnsStore);
  readonly labels: Record<string, string> = { Requested: 'Solicitat', Approved: 'Aprobat', Rejected: 'Respins', Restoring: 'Restituire stoc în curs', Restored: 'Stoc restituit' };
  constructor() { afterNextRender(() => void this.state.load(this.order().id, this.session().id)); }
}
