import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { DemoOrderPage } from '../models/demo-order-page.model';
@Injectable({ providedIn: 'root' })
export class DemoOrdersApi {
  private readonly http = inject(HttpClient);
  read(page: number) {
    return firstValueFrom(
      this.http.get<DemoOrderPage>('/api/v1/ordering/demo-orders', { params: { page } }),
    );
  }
}
