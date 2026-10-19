import { Component, input, output } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { SupplierItem } from '../../models/supplier-item.model';
@Component({
  selector: 'sd-supplier-items',
  imports: [DecimalPipe],
  templateUrl: './supplier-items.component.html',
  styleUrl: './supplier-items.component.scss',
})
export class SupplierItems {
  readonly items = input.required<SupplierItem[]>();
  readonly disabled = input(false);
  readonly edited = output<SupplierItem>();
}
