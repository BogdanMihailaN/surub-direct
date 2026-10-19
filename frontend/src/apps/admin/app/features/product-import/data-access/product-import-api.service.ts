import { inject, Injectable } from '@angular/core';
import { ApiClient } from '@shared/http/api-client.service';
import { API_ORIGIN } from '@shared/http/api-origin.token';
import { ImportPreview } from '../models/import-preview.model';
import { ImportResult } from '../models/import-result.model';
@Injectable({ providedIn: 'root' })
export class ProductImportApi {
  private readonly api = inject(ApiClient);
  readonly templateUrl = inject(API_ORIGIN) + '/api/v1/admin/products/import/template';
  preview(csv: string) {
    return this.api.write<ImportPreview>('POST', '/api/v1/admin/products/import/preview', { csv });
  }
  commit(csv: string) {
    return this.api.write<ImportResult>('POST', '/api/v1/admin/products/import/commit', { csv });
  }
}
