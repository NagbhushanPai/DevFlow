import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../core/auth.service';
@Component({ standalone: true, imports: [CommonModule, FormsModule], template: `<section class="auth-card"><h1>Welcome back</h1><p>Sign in to DevFlow.</p><form (ngSubmit)="submit()"><label>Email<input name="email" type="email" [(ngModel)]="email" required></label><label>Password<input name="password" type="password" [(ngModel)]="password" required></label><p class="error" *ngIf="error()">{{ error() }}</p><button type="submit" [disabled]="busy()">{{ busy() ? 'Signing in…' : 'Sign in' }}</button></form></section>` })
export class LoginComponent { private readonly auth = inject(AuthService); private readonly router = inject(Router); email = ''; password = ''; readonly busy = signal(false); readonly error = signal(''); submit() { this.busy.set(true); this.error.set(''); this.auth.login(this.email, this.password).subscribe({ next: () => void this.router.navigate(['/dashboard']), error: () => { this.error.set('Invalid email or password.'); this.busy.set(false); } }); } }
