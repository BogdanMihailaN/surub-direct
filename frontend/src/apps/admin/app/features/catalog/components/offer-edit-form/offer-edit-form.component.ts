import { Component, effect, input, output } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Offer, Price } from '@shared/models';
import { DecimalPipe } from '@angular/common';
import { UpdateOfferRequest } from '../../models/update-offer.request';

@Component({
  selector: 'sd-offer-edit-form',
  imports: [FormsModule, MatButtonModule, MatFormFieldModule, MatInputModule, DecimalPipe],
  templateUrl: './offer-edit-form.component.html',
  styleUrl: './offer-edit-form.component.scss',
})
export class OfferEditForm {
  readonly offer = input.required<Offer>();
  readonly sku = input.required<string>();
  readonly price = input<Price>();
  readonly busy = input(false);
  readonly submitted = output<UpdateOfferRequest>();
  readonly cancelled = output<void>();
  details: UpdateOfferRequest = { label: '', unitsPerPack: 1, minPacks: 1, packStep: 1 };
  confirmed = false;
  constructor() {
    effect(() => {
      const { label, unitsPerPack, minPacks, packStep } = this.offer();
      this.details = { label, unitsPerPack, minPacks, packStep };
      this.confirmed = false;
    });
  }
  packSizeChanged() {
    return this.details.unitsPerPack !== this.offer().unitsPerPack;
  }
  quantitiesValid() {
    return (
      [this.details.unitsPerPack, this.details.minPacks, this.details.packStep].every(
        (value) => Number.isInteger(value) && value > 0 && value <= 2147483647,
      ) && this.details.unitsPerPack * this.details.minPacks <= 2147483647
    );
  }
  submit(form: NgForm) {
    if (
      form.invalid ||
      this.busy() ||
      !this.quantitiesValid() ||
      (this.packSizeChanged() && !this.confirmed)
    )
      return;
    this.submitted.emit({ ...this.details });
  }
}
