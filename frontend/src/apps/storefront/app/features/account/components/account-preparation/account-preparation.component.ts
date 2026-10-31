import {
  afterNextRender,
  Component,
  DestroyRef,
  effect,
  ElementRef,
  inject,
  Injector,
  input,
  signal,
  viewChild,
} from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { AccountSession } from '../../models/account-session';
import { AccountCartStore } from '../../state/account-cart.store';
import { AccountPreparationStore } from '../../state/account-preparation.store';
@Component({
  selector: 'sd-account-preparation',
  imports: [DatePipe, DecimalPipe],
  providers: [AccountPreparationStore],
  templateUrl: './account-preparation.component.html',
  styleUrl: './account-preparation.component.scss',
})
export class AccountPreparation {
  readonly session = input.required<AccountSession>();
  readonly cart = inject(AccountCartStore);
  readonly state = inject(AccountPreparationStore);
  readonly confirming = signal<string | null>(null);
  private readonly injector = inject(Injector);
  private readonly confirmation = viewChild<ElementRef<HTMLElement>>('confirmation');
  readonly labels = {
    Pending: 'Rezervare în așteptare',
    Active: 'Stoc rezervat',
    Cancelling: 'Anulare în curs',
    Released: 'Rezervare eliberată',
    Expired: 'Rezervare expirată',
    Confirming: 'Confirmare în curs',
    Consumed: 'Comandă confirmată',
    Restoring: 'Restituire în curs',
    Restored: 'Stoc restituit',
  };
  constructor() {
    const destroy = inject(DestroyRef);
    effect(() => {
      const id = this.cart.account()?.preparedId;
      const blocked = this.cart.busy() || this.cart.stale();
      const accountId = this.session().id;
      this.confirming.set(null);
      this.state.clear();
      if (id && !blocked) void this.state.load(accountId, id);
    });
    destroy.onDestroy(() => this.state.clear());
    afterNextRender(() => {
      void this.state.configure();
      const timer = setInterval(() => {
        if (document.visibilityState === 'visible' && !this.state.loading()) this.refresh();
      }, 5000);
      destroy.onDestroy(() => clearInterval(timer));
    });
  }
  refresh() {
    const id = this.cart.account()?.preparedId;
    if (id && !this.cart.busy() && !this.cart.stale()) void this.state.load(this.session().id, id);
  }
  selectConfirm() {
    const p = this.state.snapshot();
    if (
      !p ||
      p.status !== 'Active' ||
      p.confirmationRequested ||
      p.cancellationRequested ||
      this.cart.busy() ||
      this.cart.stale()
    )
      return;
    this.confirming.set(p.id);
    afterNextRender(() => this.confirmation()?.nativeElement.focus(), { injector: this.injector });
  }
  confirm() {
    const p = this.state.snapshot();
    if (
      !p ||
      this.confirming() !== p.id ||
      p.status !== 'Active' ||
      p.confirmationRequested ||
      p.cancellationRequested
    )
      return;
    this.confirming.set(null);
    void this.cart.preparationAction(p.id, 'confirm');
  }
}
