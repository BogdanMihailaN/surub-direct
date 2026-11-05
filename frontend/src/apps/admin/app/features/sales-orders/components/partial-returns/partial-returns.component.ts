import { Component, input, output } from '@angular/core';
import { OrderDetail } from '../../models/order-detail.model';
import { PartialReturn } from '../../models/partial-return.model';
import { PartialReturnSelection } from '../../models/partial-return-selection.model';
import { PartialReturnAction } from '../../models/partial-return-action.model';
import { PartialReturnCreate } from '../partial-return-create/partial-return-create.component';
import { PartialReturnCard } from '../partial-return-card/partial-return-card.component';
@Component({ selector: 'sd-partial-returns', imports: [PartialReturnCreate, PartialReturnCard], templateUrl: './partial-returns.component.html', styleUrl: './partial-returns.component.scss' })
export class PartialReturnsComponent {
  readonly detail = input.required<OrderDetail>();
  readonly returns = input.required<PartialReturn[]>();
  readonly busy = input(false);
  readonly created = output<PartialReturnSelection>();
  readonly acted = output<PartialReturnAction>();
}
