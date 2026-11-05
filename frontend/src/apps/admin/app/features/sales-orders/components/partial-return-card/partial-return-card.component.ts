import { Component, input, output } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PartialReturn } from '../../models/partial-return.model';
import { PartialReturnAction } from '../../models/partial-return-action.model';
import { returnLabels } from '../../models/return-labels';
@Component({ selector: 'sd-partial-return-card', imports: [DatePipe, FormsModule], templateUrl: './partial-return-card.component.html', styleUrl: './partial-return-card.component.scss' })
export class PartialReturnCard {
  readonly data = input.required<PartialReturn>();
  readonly busy = input(false);
  readonly acted = output<PartialReturnAction>();
  readonly labels = returnLabels;
  reason = '';
  confirmed = false;
  resalable = false;
  valid() { return !this.busy() && this.confirmed && this.reason.trim().length >= 3 && this.reason.trim().length <= 500; }
  submit(target: PartialReturnAction['target']) {
    if (!this.valid() || (target === 'Receive' && !this.resalable)) return;
    this.acted.emit({ id: this.data().id, version: this.data().version, target, reason: this.reason.trim(), resalableConfirmed: this.resalable });
  }
}
