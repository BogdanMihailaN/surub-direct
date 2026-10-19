import { Component, effect, input, output } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { SupplierItem } from '../../models/supplier-item.model';
import { SupplierItemDetails } from '../../models/supplier-item-details.model';
import { CatalogVariant } from '../../models/catalog-variant.model';
@Component({
  selector: 'sd-supplier-item-form',
  imports: [FormsModule],
  templateUrl: './supplier-item-form.component.html',
  styleUrl: './supplier-item-form.component.scss',
})
export class SupplierItemForm {
  readonly item = input<SupplierItem | null>(null);
  readonly variant = input<CatalogVariant | null>(null);
  readonly busy = input(false);
  readonly saved = output<SupplierItemDetails>();
  readonly cancelled = output<void>();
  details: SupplierItemDetails = this.empty();
  constructor() {
    effect(() => {
      this.variant();
      const i = this.item();
      this.details = i
        ? {
            supplierCode: i.supplierCode,
            packLabel: i.packLabel,
            unitsPerPack: i.unitsPerPack,
            netCostPerPack: i.netCostPerPack,
            currency: i.currency,
            isActive: i.isActive,
          }
        : this.empty();
    });
  }
  private empty(): SupplierItemDetails {
    return {
      supplierCode: '',
      packLabel: '',
      unitsPerPack: 1,
      netCostPerPack: 0,
      currency: 'RON',
      isActive: true,
    };
  }
  submit(form: NgForm) {
    if (form.valid && !this.busy() && Number.isInteger(this.details.unitsPerPack))
      this.saved.emit({ ...this.details });
  }
}
