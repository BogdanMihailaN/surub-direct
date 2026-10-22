import { Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
@Component({
  selector: 'sd-order-cancellation',
  imports: [FormsModule],
  templateUrl: './order-cancellation.component.html',
  styleUrl: './order-cancellation.component.scss',
})
export class OrderCancellation {
  readonly busy = input(false);
  readonly cancelled = output<string>();
  reason = '';
  confirmed = false;
}
