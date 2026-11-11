import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../core/auth.service';
import { NotificationService } from '../core/notification.service';
import { RealtimeService } from '../core/realtime.service';

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `<section class="auth-card"><h1>Create your account</h1><p>Start your DevFlow demo workspace.</p><form (ngSubmit)="submit()"><label>First name<input name="firstName" [(ngModel)]="firstName" required></label><label>Last name<input name="lastName" [(ngModel)]="lastName" required></label><label>Email<input name="email" type="email" [(ngModel)]="email" required></label><label>Password<input name="password" type="password" [(ngModel)]="password" minlength="6" required></label><p class="error" *ngIf="error()">{{ error() }}</p><button type="submit" [disabled]="busy()">{{ busy() ? 'Creating account…' : 'Create account' }}</button></form><p class="auth-link">Already have an account? <a routerLink="/login">Sign in</a></p></section>`
})
export class RegisterComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly notifications = inject(NotificationService);
  private readonly realtime = inject(RealtimeService);
  firstName = '';
  lastName = '';
  email = '';
  password = '';
  readonly busy = signal(false);
  readonly error = signal('');

  submit() {
    this.busy.set(true);
    this.error.set('');
    this.auth.register(this.firstName.trim(), this.lastName.trim(), this.email.trim(), this.password).subscribe({
      next: () => {
        this.notifications.load();
        void this.realtime.connect();
        void this.router.navigate(['/dashboard']);
      },
      error: (response: { error?: { detail?: string; errors?: string[] } }) => {
        this.error.set(response.error?.errors?.join(' ') || response.error?.detail || 'Unable to create the account.');
        this.busy.set(false);
      }
    });
  }
}
