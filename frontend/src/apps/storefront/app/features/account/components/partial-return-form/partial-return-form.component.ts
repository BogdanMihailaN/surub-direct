import { Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AccountPartialReturns } from '../../models/account-partial-returns.model';
import { AccountPartialSelection } from '../../models/account-partial-selection.model';
@Component({ selector: 'sd-account-partial-return-form', imports: [FormsModule], templateUrl: './partial-return-form.component.html', styleUrl: './partial-return-form.component.scss' })
export class AccountPartialReturnForm {
  readonly data = input.required<AccountPartialReturns>();
  readonly email = input.required<string>();
  readonly number = input.required<string>();
  readonly submitted = output<AccountPartialSelection>();
  quantities: Record<string, number | null> = {};
  reason = '';
  confirmed = false;
  valid() {
    return this.data().canRequest && this.confirmed && this.reason.trim().length >= 3 && this.reason.trim().length <= 500
      && this.data().offers.every(x => { const n = this.quantities[x.offerId] ?? 0; return Number.isInteger(n) && n >= 0 && n <= x.availablePacks; })
      && this.data().offers.some(x => (this.quantities[x.offerId] ?? 0) > 0);
  }
  submit() {
    if (this.valid()) this.submitted.emit({ reason: this.reason.trim(), lines: this.data().offers.filter(x => (this.quantities[x.offerId] ?? 0) > 0)
      .map(x => ({ offerId: x.offerId, packs: this.quantities[x.offerId]! })) });
  }
}
