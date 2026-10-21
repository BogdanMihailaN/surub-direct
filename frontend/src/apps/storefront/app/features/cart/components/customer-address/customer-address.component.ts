import { Component, input } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { AddressControls } from '../../models/address-controls.model';
@Component({
  selector: 'sd-customer-address',
  imports: [ReactiveFormsModule],
  templateUrl: './customer-address.component.html',
  styleUrl: './customer-address.component.scss',
})
export class CustomerAddressForm {
  readonly group = input.required<FormGroup<AddressControls>>();
  readonly label = input.required<string>();
}
