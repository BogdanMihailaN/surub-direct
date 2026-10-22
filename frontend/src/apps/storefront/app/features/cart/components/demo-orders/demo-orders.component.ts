import { Component, afterNextRender, DestroyRef, inject } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { DemoOrdersStore } from '../../state/demo-orders.store';
@Component({
  selector: 'sd-demo-orders',
  imports: [DatePipe, DecimalPipe],
  providers: [DemoOrdersStore],
  templateUrl: './demo-orders.component.html',
  styleUrl: './demo-orders.component.scss',
})
export class DemoOrders {
  readonly state = inject(DemoOrdersStore);
  readonly labels = {
    Confirming: 'Confirmare în curs',
    Confirmed: 'Confirmată',
    Cancelling: 'Anulare în curs',
    Cancelled: 'Anulată',
    Failed: 'Confirmare nereușită',
  };
  constructor() {
    const destroy = inject(DestroyRef);
    afterNextRender(() => {
      void this.state.load();
      const timer = setInterval(() => {
        if (document.visibilityState === 'visible')
          void this.state.load(this.state.result()?.page ?? 1);
      }, 10000);
      destroy.onDestroy(() => clearInterval(timer));
    });
  }
}
