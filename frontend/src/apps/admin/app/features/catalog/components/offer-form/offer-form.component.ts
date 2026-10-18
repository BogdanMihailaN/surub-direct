import { Component, input, output, effect } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { StoreProduct } from '@shared/models';
import { AddOfferRequest } from '../../models/add-offer.request';
@Component({
  selector: 'sd-offer-form',
  imports: [FormsModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  templateUrl: './offer-form.component.html',
  styleUrl: './offer-form.component.scss',
})
export class OfferForm {
  readonly busy = input(false);
  readonly item = input.required<StoreProduct>();
  readonly submitted = output<AddOfferRequest>();
  offerVariant = '';
  offer = { code: '', label: 'Cutie 100 buc.', unitsPerPack: 100, minPacks: 1, packStep: 1 };
  readonly preferred = input('');
  constructor() {
    effect(() => {
      const id = this.preferred();
      if (id) this.offerVariant = id;
    });
  }
  submit(form: NgForm) {
    if (form.invalid || this.busy()) return;
    this.submitted.emit({ ...this.offer, variantId: this.offerVariant });
  }
}
