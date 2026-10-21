import { Component, afterNextRender, DestroyRef, effect, inject } from '@angular/core';
import { DecimalPipe, DatePipe } from '@angular/common';
import { CartStore } from '../../state/cart.store';
import { PreparationStore } from '../../state/preparation.store';
@Component({
  selector: 'sd-preparation',
  imports: [DecimalPipe, DatePipe],
  providers: [PreparationStore],
  templateUrl: './preparation.component.html',
  styleUrl: './preparation.component.scss',
})
export class Preparation {
  readonly cart = inject(CartStore);
  readonly state = inject(PreparationStore);
  readonly labels = {
    Pending: 'Rezervare în așteptare',
    Active: 'Stoc rezervat',
    Cancelling: 'Anulare în curs',
    Released: 'Rezervare eliberată',
    Expired: 'Rezervare expirată',
  };
  constructor() {
    const destroy = inject(DestroyRef);
    effect(() => {
      void this.state.load(this.cart.preparedId());
    });
    afterNextRender(() => {
      void this.state.configure();
      const timer = setInterval(() => {
        if (
          this.cart.preparedId() &&
          !this.cart.busy() &&
          !this.cart.stale() &&
          document.visibilityState === 'visible'
        )
          void this.state.load(this.cart.preparedId());
      }, 5000);
      destroy.onDestroy(() => clearInterval(timer));
    });
  }
}
