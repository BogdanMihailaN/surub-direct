import { inject, Injectable, signal } from '@angular/core';
import { Category } from '@shared/models';
import { errorMessage } from '@shared/http/error-message';
import { TaxonomyApi } from '../../taxonomy/data-access/taxonomy-api.service';
import { ProductImportApi } from '../data-access/product-import-api.service';
import { ImportPreview } from '../models/import-preview.model';
import { ImportResult } from '../models/import-result.model';
@Injectable()
export class ProductImportStore {
  private readonly api = inject(ProductImportApi);
  private readonly taxonomy = inject(TaxonomyApi);
  private csv = '';
  readonly templateUrl = this.api.templateUrl;
  readonly categories = signal<Category[]>([]);
  readonly categoryError = signal('');
  readonly busy = signal(false);
  readonly filename = signal('');
  readonly error = signal('');
  readonly preview = signal<ImportPreview | null>(null);
  readonly result = signal<ImportResult | null>(null);
  async loadCategories() {
    this.categoryError.set('');
    try {
      this.categories.set((await this.taxonomy.get()).categories);
    } catch (error) {
      this.categoryError.set(errorMessage(error));
    }
  }
  async select(file: File | null) {
    if (this.busy()) return;
    this.csv = '';
    this.filename.set(file?.name ?? '');
    this.preview.set(null);
    this.result.set(null);
    this.error.set('');
    if (!file) return;
    this.busy.set(true);
    try {
      if (file.size > 1048576) throw new Error('Fișierul CSV poate avea maximum 1 MB.');
      try {
        this.csv = new TextDecoder('utf-8', { fatal: true }).decode(await file.arrayBuffer());
      } catch {
        throw new Error('Salvează fișierul în format CSV UTF-8.');
      }
      this.preview.set(await this.api.preview(this.csv));
    } catch (error) {
      this.error.set(
        error instanceof Error && !(error as { status?: number }).status
          ? error.message
          : errorMessage(error),
      );
    } finally {
      this.busy.set(false);
    }
  }
  async commit() {
    if (this.busy() || !this.preview()?.canImport || this.result()) return;
    this.busy.set(true);
    this.error.set('');
    try {
      this.result.set(await this.api.commit(this.csv));
      this.preview.set(null);
      this.csv = '';
    } catch (error) {
      this.error.set(errorMessage(error));
      // A retry requires a fresh preview, including after an ambiguous network response.
      this.preview.set(null);
    } finally {
      this.busy.set(false);
    }
  }
}
