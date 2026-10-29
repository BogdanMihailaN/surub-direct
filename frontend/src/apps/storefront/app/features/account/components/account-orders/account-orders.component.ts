import {
  afterNextRender,
  Component,
  ElementRef,
  Injector,
  inject,
  input,
  viewChild,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { DemoOrder } from '../../../cart/models/demo-order.model';
import { AccountSession } from '../../models/account-session';
import { AccountOrdersStore } from '../../state/account-orders.store';
import { OrderHistoryList } from '../order-history-list/order-history-list.component';
@Component({
  selector: 'sd-account-orders',
  imports: [OrderHistoryList, RouterLink],
  providers: [AccountOrdersStore],
  templateUrl: './account-orders.component.html',
  styleUrl: './account-orders.component.scss',
})
export class AccountOrders {
  private readonly injector = inject(Injector);
  private readonly confirmation = viewChild<ElementRef<HTMLElement>>('confirmation');
  select(order: DemoOrder) {
    this.state.select(order);
    afterNextRender(() => this.confirmation()?.nativeElement.focus(), { injector: this.injector });
  }
  readonly session = input.required<AccountSession>();
  readonly state = inject(AccountOrdersStore);
  constructor() {
    afterNextRender(() => void this.state.load(this.session().id));
  }
}
