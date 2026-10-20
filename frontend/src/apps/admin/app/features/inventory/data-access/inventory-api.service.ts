import { inject, Injectable } from '@angular/core';
import { ApiClient } from '@shared/http/api-client.service';
import { StockLedger } from '../models/stock-ledger.model';
import { AdjustStockRequest } from '../models/adjust-stock.request';
@Injectable({ providedIn: 'root' })
export class InventoryApi {
  private readonly api = inject(ApiClient);
  read(id: string, page: number) {
    return this.api.get<StockLedger>(`/api/v1/admin/variants/${id}/movements?page=${page}`);
  }
  adjust(id: string, c: AdjustStockRequest) {
    return this.api.write<{ created: boolean }>(
      'POST',
      `/api/v1/admin/variants/${id}/adjustments`,
      c,
    );
  }
}
