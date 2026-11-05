import { Component, computed, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { OrderLine } from '../../models/order-line.model';
import { PartialReturn } from '../../models/partial-return.model';
import { PartialReturnSelection } from '../../models/partial-return-selection.model';
@Component({ selector: 'sd-partial-return-create', imports: [FormsModule], templateUrl: './partial-return-create.component.html', styleUrl: './partial-return-create.component.scss' })
export class PartialReturnCreate {
  readonly lines = input.required<OrderLine[]>();
  readonly returns = input.required<PartialReturn[]>();
  readonly busy = input(false);
  readonly created = output<PartialReturnSelection>();
  readonly available = computed(() => this.lines().map(line => ({ ...line, remaining: line.packs - this.returns()
    .filter(r => r.status !== 'Rejected').flatMap(r => r.lines).filter(x => x.offerId === line.offerId).reduce((sum, x) => sum + x.packs, 0) })));
  quantities: Record<string, number | null> = {};
  reason = '';
  confirmed = false;
  valid() {
    return !this.busy() && this.confirmed && this.reason.trim().length >= 3 && this.reason.trim().length <= 500
      && this.available().every(x => { const n = this.quantities[x.offerId] ?? 0; return Number.isInteger(n) && n >= 0 && n <= x.remaining; })
      && this.available().some(x => (this.quantities[x.offerId] ?? 0) > 0);
  }
  submit() {
    if (!this.valid()) return;
    this.created.emit({ reason: this.reason.trim(), lines: this.available().filter(x => (this.quantities[x.offerId] ?? 0) > 0)
      .map(x => ({ offerId: x.offerId, packs: this.quantities[x.offerId]! })) });
  }
}
