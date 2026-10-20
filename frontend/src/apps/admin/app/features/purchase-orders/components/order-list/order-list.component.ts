import { Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PurchaseOrderPage } from '../../models/purchase-order-page.model';
@Component({
  selector: 'sd-order-list',
  imports: [FormsModule],
  templateUrl: './order-list.component.html',
  styleUrl: './order-list.component.scss',
})
export class OrderList {
  readonly data = input.required<PurchaseOrderPage>();
  readonly busy = input(false);
  readonly selectedId = input('');
  readonly selected = output<string>();
  readonly create = output<void>();
  readonly filtered = output<{ search: string; status: string }>();
  readonly paged = output<number>();
  search = '';
  status = '';
}
