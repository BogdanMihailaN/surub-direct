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
  { path: '', pathMatch: 'full', redirectTo: 'catalog' },
  { path: '**', redirectTo: 'catalog' },
];
