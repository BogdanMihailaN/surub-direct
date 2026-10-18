import { Component, computed, effect, input, output } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { Category, Taxonomy } from '@shared/models';
import { categoryOptions, effectiveAttributes } from '@shared/catalog/taxonomy-helpers';
import { CategoryWriteRequest } from '../../models/category-write.request';
@Component({
  selector: 'sd-category-form',
  imports: [FormsModule],
  templateUrl: './category-form.component.html',
  styleUrl: './category-form.component.scss',
})
export class CategoryForm {
  readonly category = input<Category | null>(null);
  readonly taxonomy = input.required<Taxonomy>();
  readonly busy = input(false);
  readonly submitted = output<CategoryWriteRequest>();
  readonly options = computed(() =>
    categoryOptions(this.taxonomy().categories).filter(
      (c) => c.category.id !== this.category()?.id,
    ),
  );
  details: CategoryWriteRequest = {
    name: '',
    slug: '',
    parentId: null,
    sortOrder: 0,
    attributeIds: [],
  };
  constructor() {
    effect(() => {
      const c = this.category();
      this.details = c
        ? {
            name: c.name,
            slug: c.slug,
            parentId: c.parentId,
            sortOrder: c.sortOrder,
            attributeIds: [...c.attributeIds],
          }
        : { name: '', slug: '', parentId: null, sortOrder: 0, attributeIds: [] };
    });
  }
  inherited() {
    return this.details.parentId ? effectiveAttributes(this.taxonomy(), this.details.parentId) : [];
  }
  toggle(id: string, checked: boolean) {
    this.details.attributeIds = checked
      ? [...this.details.attributeIds, id]
      : this.details.attributeIds.filter((x) => x !== id);
  }
  submit(form: NgForm) {
    if (form.invalid || this.busy() || !Number.isInteger(this.details.sortOrder)) return;
    this.submitted.emit({ ...this.details, attributeIds: [...this.details.attributeIds] });
  }
}
