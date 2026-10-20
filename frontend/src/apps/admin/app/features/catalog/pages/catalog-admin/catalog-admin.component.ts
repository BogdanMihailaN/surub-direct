import { StockPanel } from '../../../inventory/components/stock-panel/stock-panel.component';
import { ProductImagesPanel } from '../../../product-images/components/product-images-panel/product-images-panel.component';
import { VariantAttributesForm } from '../../components/variant-attributes-form/variant-attributes-form.component';
import { VariantEditForm } from '../../components/variant-edit-form/variant-edit-form.component';
import { OfferEditForm } from '../../components/offer-edit-form/offer-edit-form.component';
import { ProductEditForm } from '../../components/product-edit-form/product-edit-form.component';
import { ProductLifecycleComponent } from '../../components/product-lifecycle/product-lifecycle.component';
import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { CatalogAdminStore } from '../../state/catalog-admin.store';
import { ProductSidebarComponent } from '../../components/product-sidebar/product-sidebar.component';
import { VariantTableComponent } from '../../components/variant-table/variant-table.component';
import { ProductCreateForm } from '../../components/product-create-form/product-create-form.component';
import { VariantForm } from '../../components/variant-form/variant-form.component';
import { OfferForm } from '../../components/offer-form/offer-form.component';
import { PriceForm } from '../../components/price-form/price-form.component';
import { ReceiptForm } from '../../components/receipt-form/receipt-form.component';
@Component({
  selector: 'sd-catalog-admin',
  imports: [
    StockPanel,
    MatButtonModule,
    ProductImagesPanel,
    ProductSidebarComponent,
    VariantTableComponent,
    ProductCreateForm,
    ProductEditForm,
    VariantEditForm,
    VariantAttributesForm,
    OfferEditForm,
    ProductLifecycleComponent,
    VariantForm,
    OfferForm,
    PriceForm,
    ReceiptForm,
  ],
  providers: [CatalogAdminStore],
  templateUrl: './catalog-admin.component.html',
  styleUrl: './catalog-admin.component.scss',
})
export class CatalogAdminComponent {
  readonly store = inject(CatalogAdminStore);
  constructor() {
    void this.store.load();
  }
}
