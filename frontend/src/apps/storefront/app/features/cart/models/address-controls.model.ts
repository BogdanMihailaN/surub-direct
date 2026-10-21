import { FormControl } from '@angular/forms';
export interface AddressControls {
  street: FormControl<string>;
  city: FormControl<string>;
  county: FormControl<string>;
  postalCode: FormControl<string>;
  country: FormControl<string>;
}
