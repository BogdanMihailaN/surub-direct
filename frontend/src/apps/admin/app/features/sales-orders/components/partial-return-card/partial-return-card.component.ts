import { Component, computed, input, output } from '@angular/core';
import { OrderDetail } from '../../models/order-detail.model';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PartialReturn } from '../../models/partial-return.model';
import { PartialReturnAction } from '../../models/partial-return-action.model';
import { returnLabels } from '../../models/return-labels';
@Component({ selector: 'sd-partial-return-card', imports: [DatePipe, DecimalPipe, FormsModule], templateUrl: './partial-return-card.component.html', styleUrl: './partial-return-card.component.scss' })
export class PartialReturnCard {
  readonly data = input.required<PartialReturn>();
  readonly detail = input.required<OrderDetail>();
  readonly refund = computed(() => this.detail().paymentSimulation.history.find(x => x.partialReturnId === this.data().id));
  readonly refundAmount = computed(() => this.data().lines.reduce((total, line) => {
    const offer = this.detail().order.lines.find(x => x.offerId === line.offerId);
    return total + Math.round((offer?.unitGross ?? 0) * 100) * line.packs;
  }, 0) / 100);
  readonly canRefund = computed(() => this.data().status === 'Restored' && !this.refund() &&
    ['Paid', 'PartiallyRefunded'].includes(this.detail().paymentSimulation.status) && this.refundAmount() > 0);
  readonly busy = input(false);
  readonly acted = output<PartialReturnAction>();
  readonly labels = returnLabels;
  reason = '';
  confirmed = false;
  resalable = false;
  valid() { return !this.busy() && this.confirmed && this.reason.trim().length >= 3 && this.reason.trim().length <= 500; }
  submit(target: PartialReturnAction['target']) {
    if (!this.valid() || (target === 'Receive' && !this.resalable)) return;
    this.acted.emit({ id: this.data().id, version: target === 'Refund' ? this.detail().paymentSimulation.version : this.data().version, target, reason: this.reason.trim(), resalableConfirmed: this.resalable });
  }
}
