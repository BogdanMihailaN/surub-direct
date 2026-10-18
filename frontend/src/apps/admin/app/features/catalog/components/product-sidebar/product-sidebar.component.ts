import { Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { StoreProduct } from '@shared/models';
@Component({
  selector: 'sd-product-sidebar',
  imports: [MatButtonModule, FormsModule],
  templateUrl: './product-sidebar.component.html',
  styleUrl: './product-sidebar.component.scss',
})
export class ProductSidebarComponent {
  readonly products = input.required<StoreProduct[]>();
  readonly selectedId = input('');
  readonly busy = input(false);
  readonly page = input(1);
  readonly pageCount = input(1);
  readonly total = input(0);
  readonly pageChanged = output<number>();
  readonly filtered = output<{ search: string; status: string }>();
  search = '';
  status = '';
  readonly selected = output<string>();
  readonly create = output<void>();
}
