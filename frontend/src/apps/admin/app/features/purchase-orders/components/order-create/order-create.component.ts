import { Component, input, output } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { Supplier } from '../../../purchasing/models/supplier.model';
import { CreatePurchaseOrderRequest } from '../../models/create-purchase-order.request';
@Component({
  selector: 'sd-order-create',
  imports: [FormsModule],
  templateUrl: './order-create.component.html',
  styleUrl: './order-create.component.scss',
})
export class OrderCreate {
  readonly suppliers = input.required<Supplier[]>();
  readonly busy = input(false);
  readonly search = output<string>();
  readonly created = output<CreatePurchaseOrderRequest>();
  query = '';
  details: CreatePurchaseOrderRequest = { supplierId: '', currency: 'RON', notes: '' };
  submit(form: NgForm) {
    if (form.valid && !this.busy()) this.created.emit({ ...this.details });
  }
}
