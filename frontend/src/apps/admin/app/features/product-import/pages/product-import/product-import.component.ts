import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { ProductImportStore } from '../../state/product-import.store';
import { ImportUpload } from '../../components/import-upload/import-upload.component';
import { ImportPreview } from '../../components/import-preview/import-preview.component';
@Component({
  selector: 'sd-product-import',
  imports: [RouterLink, MatButtonModule, ImportUpload, ImportPreview],
  providers: [ProductImportStore],
  templateUrl: './product-import.component.html',
  styleUrl: './product-import.component.scss',
})
export class ProductImportPage {
  readonly store = inject(ProductImportStore);
  constructor() {
    void this.store.loadCategories();
  }
}
