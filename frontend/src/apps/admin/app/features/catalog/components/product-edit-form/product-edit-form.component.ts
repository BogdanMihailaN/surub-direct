import { categoryOptions } from '@shared/catalog/taxonomy-helpers';
import { Category } from '@shared/models';
import { Component, effect, input, output } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Product } from '@shared/models';
import { UpdateProductRequest } from '../../models/update-product.request';

@Component({
  selector: 'sd-product-edit-form',
  imports: [FormsModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  templateUrl: './product-edit-form.component.html',
  styleUrl: './product-edit-form.component.scss',
})
export class ProductEditForm {
  readonly product = input.required<Product>();
  readonly categories = input.required<Category[]>();
  options() {
    return categoryOptions(this.categories());
  }
  readonly busy = input(false);
  readonly submitted = output<UpdateProductRequest>();
  readonly cancelled = output<void>();
  details: UpdateProductRequest = { name: '', category: '', description: '' };
  constructor() {
    effect(() => {
      const { name, category, categoryId, description } = this.product();
      this.details = { name, category, categoryId, description };
    });
  }
  submit(form: NgForm) {
    if (form.invalid || this.busy()) return;
    this.submitted.emit({ ...this.details });
  }
}
