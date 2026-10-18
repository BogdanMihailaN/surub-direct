import { inject, Injectable } from '@angular/core';
import { ApiClient } from '@shared/http/api-client.service';
import { Taxonomy } from '@shared/models';
import { CategoryWriteRequest } from '../models/category-write.request';
import { AttributeWriteRequest } from '../models/attribute-write.request';
@Injectable({ providedIn: 'root' })
export class TaxonomyApi {
  private readonly api = inject(ApiClient);
  get() {
    return this.api.get<Taxonomy>('/api/v1/admin/taxonomy');
  }
  category(id: string, command: CategoryWriteRequest) {
    return this.api.write<{ id: string }>(
      id ? 'PUT' : 'POST',
      '/api/v1/admin/taxonomy/categories' + (id ? '/' + id : ''),
      command,
    );
  }
  attribute(id: string, command: AttributeWriteRequest) {
    return this.api.write<{ id: string }>(
      id ? 'PUT' : 'POST',
      '/api/v1/admin/taxonomy/attributes' + (id ? '/' + id : ''),
      command,
    );
  }
}
