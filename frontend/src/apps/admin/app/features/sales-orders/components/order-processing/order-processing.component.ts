import { Component, input, output } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { OrderProcessing as Processing } from '../../models/order-processing.model';
@Component({
  selector: 'sd-order-processing',
  imports: [DatePipe, FormsModule],
  templateUrl: './order-processing.component.html',
  styleUrl: './order-processing.component.scss',
})
export class OrderProcessing {
  readonly data = input.required<Processing>();
  readonly editable = input(false);
  readonly busy = input(false);
  readonly changed = output<{ target: string; reason: string }>();
  readonly labels: Record<string, string> = { Pending: 'În așteptare', Processing: 'În pregătire', Ready: 'Pregătită' };
  reason = '';
  confirmed = false;
  submit() {
    if (!this.editable() || this.busy() || !this.confirmed || this.reason.trim().length < 3 || this.reason.trim().length > 500) return;
    this.changed.emit({ target: this.data().status === 'Processing' ? 'Ready' : 'Processing', reason: this.reason.trim() });
    this.reason = '';
    this.confirmed = false;
  }
}
