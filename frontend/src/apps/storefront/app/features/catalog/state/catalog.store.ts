import { computed, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, of, switchMap, tap } from 'rxjs';
import { CatalogApiService } from '@shared/catalog/catalog-api.service';
import { effectiveAttributes } from '@shared/catalog/taxonomy-helpers';
import { errorMessage } from '@shared/http/error-message';
import { AttributeFilter, StorePage, Taxonomy } from '@shared/models';
import { CatalogFilterValues } from '../components/catalog-filters/catalog-filter-values';
@Injectable()
export class CatalogStore {
  private readonly api = inject(CatalogApiService);
  private readonly router = inject(Router);
  readonly data = signal<StorePage | null>(null);
  readonly taxonomy = signal<Taxonomy>({ categories: [], attributes: [] });
  readonly loading = signal(true);
  readonly error = signal('');
  readonly activeCategoryId = signal('');
  readonly definitions = computed(() =>
    this.activeCategoryId()
      ? effectiveAttributes(this.taxonomy(), this.activeCategoryId())
      : this.taxonomy().attributes.filter((a) => a.source !== 'Custom'),
  );
  readonly categoryName = computed(
    () => this.taxonomy().categories.find((c) => c.id === this.activeCategoryId())?.name ?? '',
  );
  search = '';
  diameter = '';
  length = '';
  page = 1;
  attributes: AttributeFilter[] = [];
  constructor() {
    inject(ActivatedRoute)
      .queryParamMap.pipe(
        tap(() => {
          this.loading.set(true);
          this.error.set('');
        }),
        switchMap((params) =>
          this.api.taxonomy().pipe(
            switchMap((taxonomy) => {
              this.taxonomy.set(taxonomy);
              this.search = params.get('q') ?? '';
              this.diameter = params.get('diametru') ?? '';
              this.length = params.get('lungime') ?? '';
              this.page = Math.max(1, Number(params.get('pagina')) || 1);
              const legacy = params.get('categorie');
              this.activeCategoryId.set(
                params.get('categorieId') ||
                  taxonomy.categories.find((c) => c.name === legacy)?.id ||
                  '',
              );
              this.attributes = JSON.parse(params.get('atribute') || '[]');
              if (!Array.isArray(this.attributes)) throw new Error('Invalid filters');
              const filters: Record<string, string | number> = { page: this.page, pageSize: 12 };
              if (this.search) filters['search'] = this.search;
              if (this.activeCategoryId()) filters['categoryId'] = this.activeCategoryId();
              else if (legacy) filters['category'] = legacy;
              if (this.diameter) filters['diameter'] = this.diameter;
              if (this.length) filters['length'] = this.length;
              if (this.attributes.length) filters['attributes'] = JSON.stringify(this.attributes);
              return this.api.catalog(filters);
            }),
            catchError((error) => {
              this.error.set(errorMessage(error));
              return of(null);
            }),
          ),
        ),
        takeUntilDestroyed(inject(DestroyRef)),
      )
      .subscribe((data) => {
        this.data.set(data);
        this.loading.set(false);
      });
  }
  apply(page = 1) {
    void this.router.navigate([], {
      queryParams: {
        q: this.search || null,
        categorieId: this.activeCategoryId() || null,
        diametru: this.diameter || null,
        lungime: this.length || null,
        atribute: this.attributes.length ? JSON.stringify(this.attributes) : null,
        pagina: page === 1 ? null : page,
      },
    });
  }
  filter(value: CatalogFilterValues) {
    this.activeCategoryId.set(value.categoryId);
    this.diameter = value.diameter;
    this.length = value.length;
    this.attributes = value.attributes;
    this.apply();
  }
  reset() {
    this.search = '';
    this.activeCategoryId.set('');
    this.diameter = '';
    this.length = '';
    this.attributes = [];
    this.apply();
  }
}
