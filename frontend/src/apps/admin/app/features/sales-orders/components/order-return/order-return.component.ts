import { Component, input, output } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { OrderReturn } from '../../models/order-return.model';
@Component({ selector: 'sd-order-return', imports: [DatePipe, FormsModule], templateUrl: './order-return.component.html', styleUrl: './order-return.component.scss' })
export class OrderReturnComponent {
  readonly data = input.required<OrderReturn>();
  readonly editable = input(false);
  readonly busy = input(false);
  readonly changed = output<{ target: string; reason: string; restockConfirmed: boolean }>();
  readonly labels: Record<string, string> = { NotRequested: 'Nesolicitat', Requested: 'Solicitat', Approved: 'Aprobat', Rejected: 'Respins', Restoring: 'Restituire stoc în curs', Restored: 'Stoc restituit' };
  reason = '';
  confirmed = false;
  restockConfirmed = false;
  submit(target: string) {
    if (!this.editable() || this.busy() || !this.confirmed || this.reason.trim().length < 3 || this.reason.trim().length > 500 || (target === 'Restoring' && !this.restockConfirmed)) return;
    this.changed.emit({ target, reason: this.reason.trim(), restockConfirmed: this.restockConfirmed });
    this.reason = ''; this.confirmed = false; this.restockConfirmed = false;
  }
}
