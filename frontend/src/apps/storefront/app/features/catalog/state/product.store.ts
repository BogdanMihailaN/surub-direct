import { inject, Injectable, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Title, Meta } from '@angular/platform-browser';
import { catchError, combineLatest, of, startWith, Subject, switchMap, tap } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CatalogApiService } from '@shared/catalog/catalog-api.service';
import { errorMessage } from '@shared/http/error-message';
import { StoreProduct } from '@shared/models';

@Injectable()
export class ProductStore {
  private readonly api = inject(CatalogApiService);
  private readonly refresh = new Subject<void>();
  readonly item = signal<StoreProduct | null>(null);
  readonly error = signal('');
  readonly loading = signal(true);
  constructor() {
    const title = inject(Title);
    const meta = inject(Meta);
    combineLatest([inject(ActivatedRoute).paramMap, this.refresh.pipe(startWith(undefined))])
      .pipe(
        tap(() => {
          this.loading.set(true);
          this.error.set('');
          this.item.set(null);
          title.setTitle('Produs | Șurub Direct');
          meta.removeTag('name="description"');
        }),
        switchMap(([params]) =>
          this.api.product(params.get('slug') || '').pipe(
            catchError((error) => {
              this.error.set(
                error.status === 404
                  ? 'Produsul nu există sau nu este publicat.'
                  : errorMessage(error),
              );
              return of(null);
            }),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((item) => {
        this.item.set(item);
        this.loading.set(false);
        if (item) {
          title.setTitle(item.product.name + ' | Șurub Direct');
          meta.updateTag({ name: 'description', content: item.product.description });
        }
      });
  }
  retry() {
    if (!this.loading()) this.refresh.next();
  }
}
