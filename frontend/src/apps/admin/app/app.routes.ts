import { Routes } from '@angular/router';
import { adminGuard } from './core/auth/admin.guard';
export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/pages/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'catalog',
    canActivate: [adminGuard],
    loadComponent: () =>
      import('./features/catalog/pages/catalog-admin/catalog-admin.component').then(
        (m) => m.CatalogAdminComponent,
      ),
  },
  {
    path: 'categorii',
    canActivate: [adminGuard],
    loadComponent: () =>
      import('./features/taxonomy/pages/taxonomy-admin/taxonomy-admin.component').then(
        (m) => m.TaxonomyAdmin,
      ),
  },
  {
    path: 'import-produse',
    canActivate: [adminGuard],
    loadComponent: () =>
      import('./features/product-import/pages/product-import/product-import.component').then(
        (m) => m.ProductImportPage,
      ),
  },
  {
    path: 'furnizori',
    canActivate: [adminGuard],
    loadComponent: () =>
      import('./features/purchasing/pages/purchasing/purchasing.component').then(
        (m) => m.PurchasingPage,
      ),
  },
  {
    path: 'aprovizionare',
    canActivate: [adminGuard],
    loadComponent: () =>
      import('./features/purchase-orders/pages/purchase-orders/purchase-orders.component').then(
        (m) => m.PurchaseOrdersPage,
      ),
  },
  { path: '', pathMatch: 'full', redirectTo: 'catalog' },
  { path: '**', redirectTo: 'catalog' },
];
