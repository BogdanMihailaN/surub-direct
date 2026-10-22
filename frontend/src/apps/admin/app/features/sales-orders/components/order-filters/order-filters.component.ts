import { Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
@Component({
  selector: 'sd-sales-order-filters',
  imports: [FormsModule],
  templateUrl: './order-filters.component.html',
  styleUrl: './order-filters.component.scss',
})
export class OrderFilters {
  readonly busy = input(false);
  readonly applied = output<{ status: string; number: string }>();
  status = '';
  number = '';
}
