import { Component, inject, signal } from '@angular/core';
import { CatalogLoading } from '../../components/catalog-loading/catalog-loading.component';
import { FormsModule } from '@angular/forms';
import { Title, Meta } from '@angular/platform-browser';
import { CatalogHeroComponent } from '../../components/catalog-hero/catalog-hero.component';
import { ProductCardComponent } from '../../components/product-card/product-card.component';
import { CatalogFiltersComponent } from '../../components/catalog-filters/catalog-filters.component';
import { CatalogStore } from '../../state/catalog.store';
@Component({
  selector: 'sd-catalog',
  imports: [CatalogLoading, FormsModule, CatalogHeroComponent, ProductCardComponent, CatalogFiltersComponent],
  providers: [CatalogStore],
  templateUrl: './catalog-page.component.html',
  styleUrl: './catalog-page.component.scss',
})
export class CatalogPage {
  readonly filtersOpen = signal(false);
  readonly store = inject(CatalogStore);
  constructor() {
    inject(Title).setTitle('Șurub Direct — Șuruburi și elemente de fixare');
    inject(Meta).updateTag({
      name: 'description',
      content:
        'Descoperă catalogul Șurub Direct: șuruburi, piulițe, șaibe, ancore și dibluri. Compară variantele, ambalajele și disponibilitatea.',
    });
  }
}
