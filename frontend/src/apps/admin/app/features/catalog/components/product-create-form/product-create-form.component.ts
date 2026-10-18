import { categoryOptions } from '@shared/catalog/taxonomy-helpers';
import { Category } from '@shared/models';
import { Component, input, output, effect } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { StoreProduct } from '@shared/models';
import { CreateProductRequest } from '../../models/create-product.request';
@Component({
  selector: 'sd-product-create-form',
  imports: [FormsModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  templateUrl: './product-create-form.component.html',
  styleUrl: './product-create-form.component.scss',
})
export class ProductCreateForm {
  readonly categories = input.required<Category[]>();
  options() {
    return categoryOptions(this.categories());
  }
  readonly busy = input(false);
  readonly submitted = output<CreateProductRequest>();
  product: CreateProductRequest = {
    name: '',
    slug: '',
    category: '',
    categoryId: '',
    description: '',
  };
  submit(form: NgForm) {
    if (form.invalid || this.busy()) return;
    this.submitted.emit({ ...this.product });
  }
}
