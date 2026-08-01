import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'atas' },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'atas',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/atas/ata-list/ata-list.component').then((m) => m.AtaListComponent),
  },
  {
    path: 'atas/nova',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/atas/nova-ata/nova-ata.component').then((m) => m.NovaAtaComponent),
  },
  {
    path: 'atas/:id/sacramental',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/atas/sacramental-form/sacramental-form.component').then(
        (m) => m.SacramentalFormComponent
      ),
  },
  {
    path: 'atas/:id/batismo',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/atas/batismo-form/batismo-form.component').then(
        (m) => m.BatismoFormComponent
      ),
  },
  {
    path: 'atas/:id/preview',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/atas/ata-preview/ata-preview.component').then(
        (m) => m.AtaPreviewComponent
      ),
  },
  {
    path: 'configuracoes',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/configuracoes/configuracoes.component').then((m) => m.ConfiguracoesComponent),
  },
  { path: '**', redirectTo: 'atas' },
];
