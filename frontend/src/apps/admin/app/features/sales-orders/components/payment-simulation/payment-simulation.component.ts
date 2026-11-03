import { Component, input, output } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PaymentSimulation as Simulation } from '../../models/payment-simulation.model';
@Component({
  selector: 'sd-payment-simulation',
  imports: [DatePipe, DecimalPipe, FormsModule],
  templateUrl: './payment-simulation.component.html',
  styleUrl: './payment-simulation.component.scss',
})
export class PaymentSimulation {
  readonly data = input.required<Simulation>();
  readonly editable = input(false);
  readonly busy = input(false);
  readonly changed = output<{ action: string; reason: string }>();
  readonly labels: Record<string, string> = {
    NotStarted: 'Neinițiată', Pending: 'În curs', Paid: 'Reușită', Failed: 'Eșuată', Refunded: 'Rambursată integral',
  };
  reason = '';
  confirmed = false;
  submit(action: string) {
    if (!this.editable() || this.busy() || !this.confirmed || this.reason.trim().length < 3 || this.reason.trim().length > 500) return;
    this.changed.emit({ action, reason: this.reason.trim() });
    this.reason = '';
    this.confirmed = false;
  }
}
