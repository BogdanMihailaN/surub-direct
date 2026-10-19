import { Component, inject } from '@angular/core';
import { PurchasingStore } from '../../state/purchasing.store';
import { SupplierList } from '../../components/supplier-list/supplier-list.component';
import { SupplierForm } from '../../components/supplier-form/supplier-form.component';
import { VariantPicker } from '../../components/variant-picker/variant-picker.component';
import { SupplierItemForm } from '../../components/supplier-item-form/supplier-item-form.component';
import { SupplierItems } from '../../components/supplier-items/supplier-items.component';
@Component({
  selector: 'sd-purchasing',
  imports: [SupplierList, SupplierForm, VariantPicker, SupplierItemForm, SupplierItems],
  providers: [PurchasingStore],
  templateUrl: './purchasing.component.html',
  styleUrl: './purchasing.component.scss',
})
export class PurchasingPage {
  readonly store = inject(PurchasingStore);
  constructor() {
    void this.store.load();
  }
}
