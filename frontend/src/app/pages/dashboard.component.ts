import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../core/auth.service';

@Component({ standalone: true, imports: [RouterLink], template: `<section><h1>Dashboard</h1><p>Welcome, {{ auth.user()?.firstName }}. Your DevFlow workspace is ready.</p><div class="card-grid"><a class="card nav-card" routerLink="/organizations"><h2>Organizations & teams</h2><p>Manage your workspace structure.</p></a><a class="card nav-card" routerLink="/projects"><h2>Projects & issues</h2><p>Track work, comments, and activity.</p></a></div></section>` })
export class DashboardComponent { readonly auth = inject(AuthService); }
