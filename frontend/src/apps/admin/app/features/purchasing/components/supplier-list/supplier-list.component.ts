import { Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SupplierPage } from '../../models/supplier-page.model';
@Component({
  selector: 'sd-supplier-list',
  imports: [FormsModule],
  templateUrl: './supplier-list.component.html',
  styleUrl: './supplier-list.component.scss',
})
export class SupplierList {
  readonly data = input.required<SupplierPage>();
  readonly busy = input(false);
  readonly selectedId = input('');
  readonly selected = output<string>();
  readonly create = output<void>();
  readonly filtered = output<{ search: string; active: string }>();
  readonly paged = output<number>();
  search = '';
  active = '';
  apply() {
    this.filtered.emit({ search: this.search, active: this.active });
  }
}
