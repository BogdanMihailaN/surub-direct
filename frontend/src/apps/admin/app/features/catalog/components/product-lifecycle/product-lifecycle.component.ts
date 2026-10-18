import { Component, effect, input, output, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { Product } from '@shared/models';

@Component({
  selector: 'sd-product-lifecycle',
  imports: [MatButtonModule],
  templateUrl: './product-lifecycle.component.html',
  styleUrl: './product-lifecycle.component.scss',
})
export class ProductLifecycleComponent {
  readonly product = input.required<Product>();
  readonly busy = input(false);
  readonly archived = output<void>();
  readonly restored = output<void>();
  readonly confirming = signal(false);
  constructor() {
    effect(() => {
      this.product();
      this.confirming.set(false);
    });
  }
}
