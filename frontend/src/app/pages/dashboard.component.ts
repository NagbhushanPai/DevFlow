import { Component, inject } from '@angular/core';
import { AuthService } from '../core/auth.service';

@Component({ standalone: true, template: `<section><h1>Dashboard</h1><p>Welcome, {{ auth.user()?.firstName }}. Your DevFlow workspace is ready.</p></section>` })
export class DashboardComponent { readonly auth = inject(AuthService); }
