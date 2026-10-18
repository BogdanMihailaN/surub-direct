import { Component, computed, input, output, Input, OnChanges } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AttributeFilter, Category, TechnicalAttribute } from '@shared/models';
import { categoryOptions } from '@shared/catalog/taxonomy-helpers';
import { CatalogFilterValues } from './catalog-filter-values';
@Component({
  selector: 'sd-catalog-filters',
  imports: [FormsModule],
  templateUrl: './catalog-filters.component.html',
  styleUrl: './catalog-filters.component.scss',
})
export class CatalogFiltersComponent implements OnChanges {
  readonly categories = input.required<Category[]>();
  readonly definitions = input.required<TechnicalAttribute[]>();
  readonly options = computed(() => categoryOptions(this.categories()));
  readonly extra = computed(() =>
    this.definitions().filter((a) => a.source !== 'Diameter' && a.source !== 'Length'),
  );
  @Input() categoryId = '';
  @Input() diameter = '';
  @Input() length = '';
  @Input() attributes: AttributeFilter[] = [];
  readonly changed = output<CatalogFilterValues>();
  readonly cleared = output<void>();
  values: Record<string, { text: string; min: number | null; max: number | null }> = {};
  ngOnChanges() {
    this.values = Object.fromEntries(
      this.extra().map((a) => {
        const f = this.attributes.find((f) => f?.attributeId === a.id);
        return [a.id, { text: f?.text ?? '', min: f?.minimum ?? null, max: f?.maximum ?? null }];
      }),
    );
  }
  has(source: string) {
    return this.definitions().some((a) => a.source === source);
  }
  valid() {
    return Object.values(this.values).every(
      (v) =>
        (v.min === null || Number.isFinite(v.min)) &&
        (v.max === null || Number.isFinite(v.max)) &&
        (v.min === null || v.max === null || v.min <= v.max),
    );
  }
  choose(categoryId: string) {
    this.changed.emit({ categoryId, diameter: '', length: '', attributes: [] });
  }
  apply() {
    if (!this.valid()) return;
    const attributes = this.extra().flatMap<AttributeFilter>((a) => {
      const v = this.values[a.id];
      if (a.kind === 'Text')
        return v.text.trim() ? [{ attributeId: a.id, text: v.text.trim() }] : [];
      return v.min !== null || v.max !== null
        ? [{ attributeId: a.id, minimum: v.min ?? undefined, maximum: v.max ?? undefined }]
        : [];
    });
    this.changed.emit({
      categoryId: this.categoryId,
      diameter: String(this.diameter || ''),
      length: String(this.length || ''),
      attributes,
    });
  }
}
