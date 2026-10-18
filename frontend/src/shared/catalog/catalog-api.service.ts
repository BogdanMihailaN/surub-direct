import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { API_ORIGIN } from '../http/api-origin.token';
import { StorePage, StoreProduct, Taxonomy } from '../models';
@Injectable({ providedIn: 'root' })
export class CatalogApiService {
  private readonly http = inject(HttpClient);
  private readonly origin = inject(API_ORIGIN);
  catalog(filters: Record<string, string | number> = {}) {
    return this.http.get<StorePage>(this.origin + '/api/v1/products', {
      params: new HttpParams({ fromObject: filters }),
    });
  }
  taxonomy() {
    return this.http.get<Taxonomy>(this.origin + '/api/v1/taxonomy');
  }
  categories() {
    return this.http.get<string[]>(this.origin + '/api/v1/categories');
  }
  product(slug: string) {
    return this.http.get<StoreProduct>(
      this.origin + '/api/v1/products/' + encodeURIComponent(slug),
    );
  }
}
