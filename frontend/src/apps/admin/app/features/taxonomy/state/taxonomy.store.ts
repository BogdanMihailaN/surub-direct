import { computed, inject, Injectable, signal } from '@angular/core';
import { Taxonomy } from '@shared/models';
import { categoryOptions } from '@shared/catalog/taxonomy-helpers';
import { errorMessage } from '@shared/http/error-message';
import { TaxonomyApi } from '../data-access/taxonomy-api.service';
import { CategoryWriteRequest } from '../models/category-write.request';
import { AttributeWriteRequest } from '../models/attribute-write.request';
@Injectable()
export class TaxonomyStore {
  private readonly api = inject(TaxonomyApi);
  readonly data = signal<Taxonomy>({ categories: [], attributes: [] });
  readonly options = computed(() => categoryOptions(this.data().categories));
  readonly categoryId = signal('');
  readonly attributeId = signal('');
  readonly category = computed(
    () => this.data().categories.find((c) => c.id === this.categoryId()) ?? null,
  );
  readonly attribute = computed(
    () => this.data().attributes.find((a) => a.id === this.attributeId()) ?? null,
  );
  readonly busy = signal(false);
  readonly error = signal('');
  readonly message = signal('');
  load() {
    return this.run(async () => {
      this.data.set(await this.api.get());
    }, '');
  }
  chooseCategory(id: string) {
    if (!this.busy()) {
      this.categoryId.set(id);
      this.error.set('');
      this.message.set('');
    }
  }
  chooseAttribute(id: string) {
    if (!this.busy()) {
      this.attributeId.set(id);
      this.error.set('');
      this.message.set('');
    }
  }
  saveCategory(command: CategoryWriteRequest) {
    return this.run(async () => {
      const result = await this.api.category(this.categoryId(), command);
      this.data.set(await this.api.get());
      this.categoryId.set(result.id);
    }, 'Categoria a fost salvată.');
  }
  saveAttribute(command: AttributeWriteRequest) {
    return this.run(async () => {
      const result = await this.api.attribute(this.attributeId(), command);
      this.data.set(await this.api.get());
      this.attributeId.set(result.id);
    }, 'Atributul a fost salvat.');
  }
  private async run(operation: () => Promise<void>, message: string) {
    if (this.busy()) return;
    this.busy.set(true);
    this.error.set('');
    this.message.set('');
    try {
      await operation();
      this.message.set(message);
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.busy.set(false);
    }
  }
}
