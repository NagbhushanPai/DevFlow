import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  { path: 'login', loadComponent: () => import('./pages/login.component').then(m => m.LoginComponent) },
  { path: 'dashboard', canActivate: [authGuard], loadComponent: () => import('./pages/dashboard.component').then(m => m.DashboardComponent) },
  { path: 'organizations', canActivate: [authGuard], loadComponent: () => import('./pages/organizations.component').then(m => m.OrganizationsComponent) },
  { path: 'projects', canActivate: [authGuard], loadComponent: () => import('./pages/projects.component').then(m => m.ProjectsComponent) },
  { path: 'projects/:projectId/board', canActivate: [authGuard], loadComponent: () => import('./pages/board.component').then(m => m.BoardComponent) },
  { path: 'projects/:projectId/sprints', canActivate: [authGuard], loadComponent: () => import('./pages/sprints.component').then(m => m.SprintsComponent) },
  { path: '**', redirectTo: 'dashboard' }
];
