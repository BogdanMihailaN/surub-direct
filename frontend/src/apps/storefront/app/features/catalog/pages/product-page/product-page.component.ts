import { AddToCart } from '../../../cart/components/add-to-cart/add-to-cart.component';
import { Component, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Title, Meta } from '@angular/platform-browser';
import { switchMap, catchError, of, tap } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CatalogApiService as Api } from '@shared/catalog/catalog-api.service';
import { errorMessage } from '@shared/http/error-message';
import { StoreProduct } from '@shared/models';
import { ProductGallery } from '../../components/product-gallery/product-gallery.component';
@Component({
  selector: 'sd-product',
  imports: [RouterLink, DecimalPipe, ProductGallery, AddToCart],
  templateUrl: './product-page.component.html',
  styleUrl: './product-page.component.scss',
})
export class ProductPage {
  readonly api = inject(Api);
  readonly item = signal<StoreProduct | null>(null);
  readonly error = signal('');
  constructor() {
    const title = inject(Title);
    const meta = inject(Meta);
    inject(ActivatedRoute)
      .paramMap.pipe(
        tap(() => {
          this.error.set('');
          this.item.set(null);
        }),
        switchMap((p) =>
          this.api.product(p.get('slug') || '').pipe(
            catchError((e) => {
              this.error.set(
                e.status === 404 ? 'Produsul nu există sau nu este publicat.' : errorMessage(e),
              );
              return of(null);
            }),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((x) => {
        this.item.set(x);
        if (x) {
          title.setTitle(x.product.name + ' | Șurub Direct');
          meta.updateTag({ name: 'description', content: x.product.description });
        }
      });
  }
  price(id: string) {
    return this.item()?.prices.find((x) => x.offerId === id);
  }
  available(id: string) {
    return this.item()?.stock.find((x) => x.variantId === id)?.available || 0;
  }
  packs(n: number, u: number) {
    return Math.floor(n / u);
  }
}
