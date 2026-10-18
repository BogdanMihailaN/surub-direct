import { Component, input, output, effect } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { StoreProduct } from '@shared/models';
import { SetPriceRequest } from '../../models/set-price.request';
@Component({
  selector: 'sd-price-form',
  imports: [FormsModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  templateUrl: './price-form.component.html',
  styleUrl: './price-form.component.scss',
})
export class PriceForm {
  readonly busy = input(false);
  readonly item = input.required<StoreProduct>();
  readonly submitted = output<SetPriceRequest>();
  priceOffer = '';
  netAmount: number | null = null;
  taxPercent: number | null = null;
  readonly preferred = input('');
  constructor() {
    effect(() => {
      const id = this.preferred();
      if (id && id !== this.priceOffer) {
        this.priceOffer = id;
        this.fillPrice();
      }
    });
  }
  fillPrice() {
    const price = this.item().prices.find((x) => x.offerId === this.priceOffer);
    this.netAmount = price?.netAmount ?? null;
    this.taxPercent = price ? price.taxRate * 100 : null;
  }
  submit(form: NgForm) {
    if (form.invalid || this.busy()) return;
    if (this.netAmount === null || this.taxPercent === null) return;
    this.submitted.emit({
      offerId: this.priceOffer,
      netAmount: this.netAmount,
      taxRate: this.taxPercent / 100,
    });
  }
}
