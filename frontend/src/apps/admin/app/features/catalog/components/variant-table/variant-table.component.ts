import { Component, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { DecimalPipe } from '@angular/common';
import { StoreProduct } from '@shared/models';
@Component({
  selector: 'sd-variant-table',
  imports: [DecimalPipe, MatButtonModule],
  templateUrl: './variant-table.component.html',
  styleUrl: './variant-table.component.scss',
})
export class VariantTableComponent {
  readonly item = input.required<StoreProduct>();
  readonly disabled = input(false);
  readonly attributesEdited = output<string>();
  readonly variantEdited = output<string>();
  readonly offerEdited = output<{ variantId: string; offerId: string }>();
  stock(id: string) {
    return this.item().stock.find((x) => x.variantId === id)?.available || 0;
  }
  price(id: string) {
    return this.item().prices.find((x) => x.offerId === id);
  }
}
