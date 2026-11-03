import { Component, input, output } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Shipment } from '../../models/shipment.model';
@Component({
  selector: 'sd-shipment', imports: [DatePipe, FormsModule],
  templateUrl: './shipment.component.html', styleUrl: './shipment.component.scss',
})
export class ShipmentComponent {
  readonly data = input.required<Shipment>();
  readonly editable = input(false);
  readonly paid = input(false);
  readonly busy = input(false);
  readonly changed = output<{ target: string; reason: string }>();
  readonly labels: Record<string, string> = { NotStarted: 'Necreată', Ready: 'Pregătită pentru expediere', Shipped: 'Expediată', Delivered: 'Livrată' };
  readonly actions: Record<string, string> = { NotStarted: 'Creează expedierea demo', Ready: 'Marchează expediată demo', Shipped: 'Marchează livrată demo' };
  reason = '';
  confirmed = false;
  submit() {
    if (!this.editable() || this.busy() || !this.confirmed || this.reason.trim().length < 3 || this.reason.trim().length > 500
        || (this.data().status === 'Ready' && !this.paid())) return;
    const target: Record<string, string> = { NotStarted: 'Ready', Ready: 'Shipped', Shipped: 'Delivered' };
    if (!target[this.data().status]) return;
    this.changed.emit({ target: target[this.data().status], reason: this.reason.trim() });
    this.reason = ''; this.confirmed = false;
  }
}
