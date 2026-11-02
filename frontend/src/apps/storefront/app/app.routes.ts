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
  {
    path: 'cos',
    loadComponent: () =>
      import('./features/cart/pages/cart/cart.component').then((m) => m.CartPage),
  },
  {
    path: 'cont',
    loadComponent: () =>
      import('./features/account/pages/account/account.component').then((m) => m.AccountPage),
  },
  {
    path: 'cont/recuperare',
    data: { mode: 'request' },
    loadComponent: () =>
      import('./features/account/pages/recovery/recovery.component').then((m) => m.RecoveryPage),
  },
  {
    path: 'cont/confirma-email',
    data: { mode: 'email' },
    loadComponent: () =>
      import('./features/account/pages/recovery/recovery.component').then((m) => m.RecoveryPage),
  },
  {
    path: 'cont/reseteaza-parola',
    data: { mode: 'password' },
    loadComponent: () =>
      import('./features/account/pages/recovery/recovery.component').then((m) => m.RecoveryPage),
  },
  { path: '**', redirectTo: '' },
];
