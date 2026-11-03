import { Component, input } from '@angular/core';
import { DemoOrder } from '../../models/demo-order.model';
@Component({ selector: 'sd-return-summary', templateUrl: './return-summary.component.html', styleUrl: './return-summary.component.scss' })
export class ReturnSummary {
  readonly data = input<DemoOrder['return']>();
  readonly labels: Record<string, string> = { NotRequested: 'Nesolicitat', Requested: 'Solicitat', Approved: 'Aprobat', Rejected: 'Respins', Restoring: 'Restituire stoc în curs', Restored: 'Stoc restituit' };
}
