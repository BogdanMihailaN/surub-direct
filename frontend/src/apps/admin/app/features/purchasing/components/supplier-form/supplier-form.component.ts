import { Component, effect, input, output } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { Supplier } from '../../models/supplier.model';
import { SupplierDetails } from '../../models/supplier-details.model';
@Component({
  selector: 'sd-supplier-form',
  imports: [FormsModule],
  templateUrl: './supplier-form.component.html',
  styleUrl: './supplier-form.component.scss',
})
export class SupplierForm {
  readonly supplier = input<Supplier | null>(null);
  readonly busy = input(false);
  readonly saved = output<SupplierDetails>();
  details: SupplierDetails = this.empty();
  constructor() {
    effect(() => {
      const s = this.supplier();
      this.details = s
        ? {
            name: s.name,
            countryCode: s.countryCode,
            taxId: s.taxId,
            contactName: s.contactName,
            email: s.email,
            phone: s.phone,
            address: s.address,
            notes: s.notes,
            isActive: s.isActive,
          }
        : this.empty();
    });
  }
  private empty(): SupplierDetails {
    return {
      name: '',
      countryCode: 'RO',
      taxId: '',
      contactName: '',
      email: '',
      phone: '',
      address: '',
      notes: '',
      isActive: true,
    };
  }
  submit(form: NgForm) {
    if (form.valid && !this.busy()) this.saved.emit({ ...this.details });
  }
}
