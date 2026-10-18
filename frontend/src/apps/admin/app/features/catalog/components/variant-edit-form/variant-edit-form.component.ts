import { Component, effect, input, output } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Variant } from '@shared/models';
import { UpdateVariantRequest } from '../../models/update-variant.request';

@Component({
  selector: 'sd-variant-edit-form',
  imports: [FormsModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  templateUrl: './variant-edit-form.component.html',
  styleUrl: './variant-edit-form.component.scss',
})
export class VariantEditForm {
  readonly variant = input.required<Variant>();
  readonly busy = input(false);
  readonly submitted = output<UpdateVariantRequest>();
  readonly cancelled = output<void>();
  details: UpdateVariantRequest = {
    label: '',
    diameterMm: null,
    lengthMm: null,
    material: '',
    finish: '',
  };
  constructor() {
    effect(() => {
      const { label, diameterMm, lengthMm, material, finish } = this.variant();
      this.details = { label, diameterMm, lengthMm, material, finish };
    });
  }
  dimensionsValid() {
    return [this.details.diameterMm, this.details.lengthMm].every(
      (value) =>
        value === null ||
        (Number.isFinite(value) &&
          value > 0 &&
          value <= 999999999.999 &&
          Math.abs(Math.round(value * 1000) - value * 1000) < 0.000001),
    );
  }
  submit(form: NgForm) {
    if (form.invalid || this.busy() || !this.dimensionsValid()) return;
    this.submitted.emit({ ...this.details });
  }
}
