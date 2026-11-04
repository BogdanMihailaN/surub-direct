import { Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DemoOrder } from '../../../cart/models/demo-order.model';
@Component({ selector: 'sd-return-request', imports: [FormsModule], templateUrl: './return-request.component.html', styleUrl: './return-request.component.scss' })
export class ReturnRequest {
  readonly order = input.required<DemoOrder>();
  readonly email = input.required<string>();
  readonly busy = input(false);
  readonly submitted = output<string>();
  readonly cancelled = output<void>();
  reason = '';
  confirmed = false;
  submit() {
    const reason = this.reason.trim();
    if (!this.busy() && this.confirmed && reason.length >= 3 && reason.length <= 500) this.submitted.emit(reason);
  }
}
