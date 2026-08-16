import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth.service';
@Component({ selector: 'app-root', standalone: true, imports: [CommonModule, RouterOutlet, RouterLink], template: `<header><a routerLink="/dashboard" class="brand">DevFlow</a><button *ngIf="auth.isAuthenticated()" (click)="auth.logout()">Sign out</button></header><main><router-outlet /></main>` })
export class AppComponent { readonly auth = inject(AuthService); }
