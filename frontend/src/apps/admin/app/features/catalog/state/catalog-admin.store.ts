import { computed, inject, Injectable, signal } from '@angular/core';
import { AdminCatalogApi } from '../data-access/admin-catalog-api.service';
import { TaxonomyApi } from '../../taxonomy/data-access/taxonomy-api.service';
import { effectiveAttributes } from '@shared/catalog/taxonomy-helpers';
import { Taxonomy } from '@shared/models';
import { StoreProduct } from '@shared/models';
import { errorMessage } from '@shared/http/error-message';
import { SetVariantAttributeRequest } from '../models/set-variant-attribute.request';
import { UpdateVariantRequest } from '../models/update-variant.request';
import { UpdateOfferRequest } from '../models/update-offer.request';
import { CatalogEditor } from '../models/catalog-editor.model';
import { UpdateProductRequest } from '../models/update-product.request';
import { CreateProductRequest } from '../models/create-product.request';
import { AddVariantRequest } from '../models/add-variant.request';
import { AddOfferRequest } from '../models/add-offer.request';
import { SetPriceRequest } from '../models/set-price.request';
import { ReceiveStockRequest } from '../models/receive-stock.request';
@Injectable()
export class CatalogAdminStore {
  private readonly api = inject(AdminCatalogApi);
  private readonly taxonomyApi = inject(TaxonomyApi);
  readonly taxonomy = signal<Taxonomy>({ categories: [], attributes: [] });
  readonly attributeDefinitions = computed(() =>
    effectiveAttributes(this.taxonomy(), this.selected()?.product.categoryId ?? ''),
  );
  readonly products = signal<StoreProduct[]>([]);
  readonly selected = signal<StoreProduct | null>(null);
  readonly selectedId = computed(() => this.selected()?.product.id ?? '');
  readonly editor = signal<CatalogEditor | null>(null);
  readonly editing = computed(() => this.editor() !== null);
  readonly editingVariant = computed(() => {
    const editor = this.editor();
    if (!editor || editor.kind === 'product') return undefined;
    return this.selected()?.product.variants.find((v) => v.id === editor.variantId);
  });
  readonly editingOffer = computed(() => {
    const editor = this.editor();
    return editor?.kind === 'offer'
      ? this.editingVariant()?.offers.find((o) => o.id === editor.offerId)
      : undefined;
  });
  readonly editingPrice = computed(() =>
    this.selected()?.prices.find((p) => p.offerId === this.editingOffer()?.id),
  );
  readonly page = signal(1);
  readonly total = signal(0);
  readonly search = signal('');
  readonly status = signal('');
  readonly pageCount = computed(() => Math.max(1, Math.ceil(this.total() / 25)));
  readonly creating = signal(false);
  readonly busy = signal(false);
  readonly error = signal('');
  readonly message = signal('');
  readonly preferredVariant = signal('');
  readonly preferredOffer = signal('');
  async load() {
    await this.run(async () => {
      this.taxonomy.set(await this.taxonomyApi.get());
      await this.reload();
    }, '');
  }
  private async reload(
    id = this.selectedId(),
    page = this.page(),
    search = this.search(),
    status = this.status(),
  ) {
    let [result, selected] = await Promise.all([
      this.api.list(page, search, status),
      id ? this.api.get(id) : Promise.resolve(null),
    ]);
    const lastPage = Math.max(1, Math.ceil(result.total / 25));
    if (result.page > lastPage) result = await this.api.list(lastPage, search, status);
    this.products.set(result.items);
    this.total.set(result.total);
    this.page.set(result.page);
    this.search.set(search);
    this.status.set(status);
    this.selected.set(selected);
  }
  refreshProduct(id: string) {
    if (id === this.selectedId()) return this.run(() => this.reload(), '');
    return Promise.resolve();
  }
  choose(id: string) {
    return this.run(async () => {
      this.selected.set(await this.api.get(id));
      this.creating.set(false);
      this.editor.set(null);
      this.preferredVariant.set('');
      this.preferredOffer.set('');
    }, '');
  }
  filter(search: string, status: string) {
    return this.run(async () => {
      await this.reload('', 1, search.trim(), status);
      this.creating.set(false);
      this.editor.set(null);
    }, '');
  }
  changePage(page: number) {
    if (page < 1 || page > this.pageCount()) return;
    return this.run(async () => {
      await this.reload('', page);
      this.creating.set(false);
      this.editor.set(null);
    }, '');
  }
  edit() {
    if (!this.busy()) this.editor.set({ kind: 'product' });
  }
  editAttributes(variantId: string) {
    if (this.busy() || this.editing() || this.selected()?.product.status === 'Archived') return;
    this.editor.set({ kind: 'attributes', variantId });
    this.error.set('');
    this.message.set('');
  }
  setAttributes(command: SetVariantAttributeRequest[]) {
    const editor = this.editor();
    if (editor?.kind !== 'attributes') return;
    return this.run(async () => {
      await this.api.setAttributes(this.selectedId(), editor.variantId, command);
      await this.reload();
      this.editor.set(null);
    }, 'Atributele tehnice au fost salvate.');
  }
  editVariant(variantId: string) {
    if (this.busy() || this.editing() || this.selected()?.product.status === 'Archived') return;
    this.editor.set({ kind: 'variant', variantId });
    this.message.set('');
    this.error.set('');
  }
  editOffer(target: { variantId: string; offerId: string }) {
    if (this.busy() || this.editing() || this.selected()?.product.status === 'Archived') return;
    this.editor.set({ kind: 'offer', ...target });
    this.message.set('');
    this.error.set('');
  }
  updateVariant(command: UpdateVariantRequest) {
    const editor = this.editor();
    if (editor?.kind !== 'variant') return;
    return this.run(async () => {
      await this.api.updateVariant(this.selectedId(), editor.variantId, command);
      await this.reload();
      this.editor.set(null);
    }, 'Varianta a fost actualizată. SKU-ul și stocul au fost păstrate.');
  }
  updateOffer(command: UpdateOfferRequest) {
    const editor = this.editor();
    if (editor?.kind !== 'offer') return;
    let packSizeChanged = false;
    return this.run(
      async () => {
        ({ packSizeChanged } = await this.api.updateOffer(
          this.selectedId(),
          editor.variantId,
          editor.offerId,
          command,
        ));
        await this.reload();
        this.editor.set(null);
        this.preferredOffer.set(editor.offerId);
      },
      () =>
        packSizeChanged
          ? 'Ambalaj actualizat. Produsul este ciornă; verifică prețul pe ambalaj și republică.'
          : 'Ambalaj actualizat. Codul și prețul au fost păstrate.',
    );
  }
  cancelEdit() {
    if (!this.busy()) this.editor.set(null);
  }
  update(command: UpdateProductRequest) {
    return this.run(async () => {
      await this.api.update(this.selectedId(), command);
      await this.reload();
      this.editor.set(null);
    }, 'Datele produsului au fost salvate.');
  }
  archive() {
    return this.run(async () => {
      await this.api.archive(this.selectedId());
      await this.reload();
    }, 'Produs arhivat. Nu mai este vizibil în magazin.');
  }
  restore() {
    return this.run(async () => {
      await this.api.restore(this.selectedId());
      await this.reload();
    }, 'Produs restaurat ca ciornă. Verifică datele înainte de publicare.');
  }
  newProduct() {
    if (this.busy()) return;
    this.selected.set(null);
    this.editor.set(null);
    this.creating.set(true);
    this.message.set('');
    this.error.set('');
  }
  private async run(operation: () => Promise<void>, message: string | (() => string)) {
    if (this.busy()) return;
    this.busy.set(true);
    this.error.set('');
    this.message.set('');
    try {
      await operation();
      this.message.set(typeof message === 'string' ? message : message());
    } catch (e) {
      this.error.set(errorMessage(e));
    } finally {
      this.busy.set(false);
    }
  }
  create(command: CreateProductRequest) {
    return this.run(async () => {
      const result = await this.api.create(command);
      await this.reload(result.id);
      this.creating.set(false);
      this.preferredVariant.set('');
      this.preferredOffer.set('');
    }, 'Produs creat ca ciornă. Adaugă o variantă.');
  }
  addVariant(command: AddVariantRequest) {
    return this.run(async () => {
      const result = await this.api.addVariant(this.selectedId(), command);
      await this.reload();
      this.preferredVariant.set(result.id);
    }, 'Variantă creată. Configurează ambalajul.');
  }
  addOffer(command: AddOfferRequest) {
    return this.run(async () => {
      const result = await this.api.addOffer(this.selectedId(), command);
      await this.reload();
      this.preferredOffer.set(result.id);
    }, 'Ambalaj creat. Completează prețul.');
  }
  setPrice(command: SetPriceRequest) {
    return this.run(async () => {
      await this.api.setPrice(command);
      await this.reload();
    }, 'Preț actualizat.');
  }
  receive(command: ReceiveStockRequest) {
    return this.run(async () => {
      await this.api.receive(command);
      await this.reload();
    }, 'Recepție înregistrată. Stocul a fost actualizat.');
  }
  publish() {
    return this.run(async () => {
      await this.api.publish(this.selectedId());
      await this.reload();
    }, 'Produs publicat în magazin.');
  }
}
