import {
  afterNextRender,
  Component,
  effect,
  ElementRef,
  inject,
  Injector,
  input,
  signal,
  viewChild,
} from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AccountSession } from '../../models/account-session';
import { AccountCartStore } from '../../state/account-cart.store';
import { CartLine } from '../../../cart/components/cart-line/cart-line.component';
import { CartQuoteStore } from '../../../cart/state/cart-quote.store';
@Component({
  selector: 'sd-account-cart',
  imports: [CartLine, FormsModule, RouterLink, DatePipe, DecimalPipe],
  providers: [AccountCartStore, CartQuoteStore],
  templateUrl: './account-cart.component.html',
  styleUrl: './account-cart.component.scss',
})
export class AccountCart {
  readonly session = input.required<AccountSession>();
  readonly state = inject(AccountCartStore);
  readonly pricing = inject(CartQuoteStore);
  readonly includeCustomer = signal(false);
  private readonly injector = inject(Injector);
  private readonly confirmation = viewChild<ElementRef<HTMLElement>>('confirmation');
  constructor() {
    afterNextRender(() => void this.state.load(this.session().id));
    effect(() => {
      void this.pricing.load(this.state.locked() ? [] : (this.state.account()?.lines ?? []));
    });
  }
  selectTransfer() {
    this.includeCustomer.set(false);
    this.state.selectTransfer();
    this.focusConfirmation();
  }
  selectRestart() {
    this.state.selectRestart();
    this.focusConfirmation();
  }
  private focusConfirmation() {
    afterNextRender(() => this.confirmation()?.nativeElement.focus(), { injector: this.injector });
  }
  detail(offerId: string) {
    return this.pricing.quote()?.lines.find((line) => line.offerId === offerId);
  }
}
