import { Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CatalogVariant } from '../../models/catalog-variant.model';
@Component({
  selector: 'sd-purchasing-variant-picker',
  imports: [FormsModule],
  templateUrl: './variant-picker.component.html',
  styleUrl: './variant-picker.component.scss',
})
export class VariantPicker {
  readonly results = input.required<CatalogVariant[]>();
  readonly busy = input(false);
  readonly searched = input(false);
  readonly search = output<string>();
  readonly selected = output<CatalogVariant>();
  query = '';
}
