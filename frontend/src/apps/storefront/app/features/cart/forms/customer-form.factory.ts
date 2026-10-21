import { FormControl, FormGroup, Validators } from '@angular/forms';
import { AddressControls } from '../models/address-controls.model';
const text = (max: number) =>
  new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.maxLength(max), Validators.pattern(/.*\S.*/)],
  });
export function createAddressForm() {
  return new FormGroup<AddressControls>({
    street: text(200),
    city: text(100),
    county: text(100),
    postalCode: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(/^[0-9]{6}$/)],
    }),
    country: new FormControl('RO', { nonNullable: true }),
  });
}
export function createCustomerForm() {
  return new FormGroup({
    customerType: new FormControl<'person' | 'company'>('person', { nonNullable: true }),
    fullName: text(150),
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email, Validators.maxLength(254)],
    }),
    phone: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.maxLength(30),
        Validators.pattern(/^\+?[0-9 ()-]+$/),
      ],
    }),
    companyName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(200)],
    }),
    taxId: new FormControl('', {
      nonNullable: true,
      validators: [Validators.pattern(/^(RO|ro)?[0-9]{2,10}$/)],
    }),
    registrationNumber: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(40)],
    }),
    shippingAddress: createAddressForm(),
    billingSameAsShipping: new FormControl(true, { nonNullable: true }),
    billingAddress: createAddressForm(),
  });
}
