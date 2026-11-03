import { Injectable, inject } from '@angular/core';
import { ApiClient } from '@shared/http/api-client.service';
import { DeliveryCharge } from '@shared/delivery/delivery-charge.model';
@Injectable({ providedIn: 'root' })
export class DeliveryApi {
  private readonly api = inject(ApiClient);
  options() { return this.api.get<DeliveryCharge[]>('/api/v1/ordering/delivery-options'); }
}
