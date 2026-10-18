import { Component, input, output, effect } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { StoreProduct } from '@shared/models';
import { AddVariantRequest } from '../../models/add-variant.request';
@Component({
  selector: 'sd-variant-form',
  imports: [FormsModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  templateUrl: './variant-form.component.html',
  styleUrl: './variant-form.component.scss',
})
export class VariantForm {
  readonly busy = input(false);
  readonly item = input.required<StoreProduct>();
  readonly submitted = output<AddVariantRequest>();
  variant: AddVariantRequest = {
    sku: '',
    label: '',
    diameterMm: null,
    lengthMm: null,
    material: 'Oțel',
    finish: 'Zincat',
  };
  submit(form: NgForm) {
    if (form.invalid || this.busy()) return;
    this.submitted.emit({ ...this.variant });
  }
}
