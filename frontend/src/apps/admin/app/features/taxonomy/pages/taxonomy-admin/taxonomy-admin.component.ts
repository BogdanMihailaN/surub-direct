import { Component, inject } from '@angular/core';
import { TaxonomyStore } from '../../state/taxonomy.store';
import { CategoryForm } from '../../components/category-form/category-form.component';
import { AttributeForm } from '../../components/attribute-form/attribute-form.component';
@Component({
  selector: 'sd-taxonomy-admin',
  imports: [CategoryForm, AttributeForm],
  providers: [TaxonomyStore],
  templateUrl: './taxonomy-admin.component.html',
  styleUrl: './taxonomy-admin.component.scss',
})
export class TaxonomyAdmin {
  readonly store = inject(TaxonomyStore);
  constructor() {
    void this.store.load();
  }
}
