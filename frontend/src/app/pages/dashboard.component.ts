import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../core/auth.service';

@Component({ standalone: true, imports: [RouterLink], template: `<section><div class="page-heading"><div><p class="eyebrow">Workspace overview</p><h1>Dashboard</h1><p>Welcome, {{ auth.user()?.firstName }}. Your DevFlow workspace is ready.</p></div><a class="button" routerLink="/projects">Create a project</a></div><div class="card-grid"><a class="card nav-card" routerLink="/organizations"><h2>Organizations & teams</h2><p>Manage your workspace structure.</p><span class="card-action">Open organizations →</span></a><a class="card nav-card" routerLink="/projects"><h2>Projects & issues</h2><p>Track work, comments, and activity.</p><span class="card-action">View projects →</span></a></div></section>` })
export class DashboardComponent { readonly auth = inject(AuthService); }
