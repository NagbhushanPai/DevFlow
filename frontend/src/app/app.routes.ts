import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  {
    path: 'login',
    canActivate: [guestGuard],
    title: 'Sign in · DevFlow',
    loadComponent: () => import('./pages/auth/login.component').then(m => m.LoginComponent),
  },
  {
    path: 'register',
    canActivate: [guestGuard],
    title: 'Create account · DevFlow',
    loadComponent: () => import('./pages/auth/register.component').then(m => m.RegisterComponent),
  },
  {
    path: '',
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', title: 'Dashboard · DevFlow', loadComponent: () => import('./pages/dashboard.component').then(m => m.DashboardComponent) },
      { path: 'organizations', title: 'Organizations · DevFlow', loadComponent: () => import('./pages/organizations/organization-list.component').then(m => m.OrganizationListComponent) },
      { path: 'organizations/:organizationId', title: 'Organization · DevFlow', loadComponent: () => import('./pages/organizations/organization-detail.component').then(m => m.OrganizationDetailComponent) },
      { path: 'projects', title: 'Projects · DevFlow', loadComponent: () => import('./pages/projects/project-list.component').then(m => m.ProjectListComponent) },
      {
        path: 'projects/:projectId',
        loadComponent: () => import('./pages/projects/project-shell.component').then(m => m.ProjectShellComponent),
        children: [
          { path: '', pathMatch: 'full', redirectTo: 'issues' },
          { path: 'issues', title: 'Issues · DevFlow', loadComponent: () => import('./pages/projects/issue-list.component').then(m => m.IssueListComponent) },
          { path: 'issues/:issueId', title: 'Issue · DevFlow', loadComponent: () => import('./pages/projects/issue-detail.component').then(m => m.IssueDetailComponent) },
          { path: 'board', title: 'Board · DevFlow', loadComponent: () => import('./pages/projects/board.component').then(m => m.BoardComponent) },
          { path: 'sprints', title: 'Sprints · DevFlow', loadComponent: () => import('./pages/projects/sprints.component').then(m => m.SprintsComponent) },
          { path: 'settings', title: 'Project settings · DevFlow', loadComponent: () => import('./pages/projects/project-settings.component').then(m => m.ProjectSettingsComponent) },
        ],
      },
    ],
  },
  { path: '**', title: 'Not found · DevFlow', loadComponent: () => import('./pages/not-found.component').then(m => m.NotFoundComponent) },
];
