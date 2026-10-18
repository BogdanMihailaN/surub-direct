import { Routes } from '@angular/router';
export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/catalog/pages/catalog-page/catalog-page.component').then(
        (m) => m.CatalogPage,
      ),
  },
  {
    path: 'produse/:slug',
    loadComponent: () =>
      import('./features/catalog/pages/product-page/product-page.component').then(
        (m) => m.ProductPage,
      ),
  },
  { path: '**', redirectTo: '' },
];
