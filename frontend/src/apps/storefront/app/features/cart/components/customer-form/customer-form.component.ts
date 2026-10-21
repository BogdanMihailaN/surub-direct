import { Component, effect, inject, signal } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CartStore } from '../../state/cart.store';
import { createCustomerForm } from '../../forms/customer-form.factory';
import { CustomerDraft } from '../../models/customer-draft.model';
import { CustomerAddressForm } from '../customer-address/customer-address.component';
@Component({
  selector: 'sd-customer-form',
  imports: [ReactiveFormsModule, CustomerAddressForm],
  templateUrl: './customer-form.component.html',
  styleUrl: './customer-form.component.scss',
})
export class CustomerForm {
  readonly cart = inject(CartStore);
  readonly form = createCustomerForm();
  readonly message = signal('');
  readonly attempted = signal(false);
  constructor() {
    this.updateFields();
    this.form.controls.customerType.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.updateFields());
    this.form.controls.billingSameAsShipping.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.updateFields());
    effect(() => {
      const customer = this.cart.customer();
      if (!this.form.dirty) this.fill(customer);
    });
  }
  private updateFields() {
    const c = this.form.controls;
    for (const field of [c.companyName, c.taxId, c.registrationNumber])
      if (c.customerType.value === 'company') field.enable({ emitEvent: false });
      else field.disable({ emitEvent: false });
    if (c.billingSameAsShipping.value) c.billingAddress.disable({ emitEvent: false });
    else c.billingAddress.enable({ emitEvent: false });
  }
  private fill(customer: CustomerDraft | null) {
    this.form.reset();
    if (customer)
      this.form.patchValue({
        ...customer,
        companyName: customer.companyName ?? '',
        taxId: customer.taxId ?? '',
        registrationNumber: customer.registrationNumber ?? '',
        billingAddress: customer.billingAddress ?? undefined,
      });
    this.updateFields();
    this.form.markAsPristine();
  }
  async save() {
    this.attempted.set(true);
    this.message.set('');
    const value = this.form.getRawValue();
    if (
      this.form.invalid ||
      this.cart.locked() ||
      (value.customerType === 'company' && (!value.companyName.trim() || !value.taxId.trim()))
    ) {
      this.form.markAllAsTouched();
      return;
    }
    const customer: CustomerDraft = {
      ...value,
      companyName: value.customerType === 'company' ? value.companyName : null,
      taxId: value.customerType === 'company' ? value.taxId : null,
      registrationNumber: value.customerType === 'company' ? value.registrationNumber : null,
      billingAddress: value.billingSameAsShipping ? null : value.billingAddress,
    };
    if (await this.cart.saveCustomer(customer)) {
      this.fill(this.cart.customer());
      this.attempted.set(false);
      this.message.set('Datele au fost salvate în ciornă. Nu a fost trimisă nicio comandă.');
    }
  }
  async clear() {
    if (await this.cart.saveCustomer(null)) {
      this.fill(null);
      this.attempted.set(false);
      this.message.set('Datele clientului au fost șterse.');
    }
  }
}
