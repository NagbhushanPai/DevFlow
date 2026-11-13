import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { FormErrors, toFormErrors } from '../../core/problem';

@Component({
  selector: 'app-login',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, RouterLink],
  template: `
    <section class="auth-page">
      <div class="auth-card">
        <h1>Welcome back</h1>
        <p>Sign in to your DevFlow workspace.</p>

        @if (reason() === 'expired') {
          <div class="alert info">Your session expired. Please sign in again.</div>
        }
        @if (errors().message) {
          <div class="alert error" role="alert">{{ errors().message }}</div>
        }

        <form (ngSubmit)="submit()" #form="ngForm" novalidate>
          <div class="field">
            <label for="email">Email</label>
            <input id="email" class="input" name="email" type="email" autocomplete="username" required email
                   [(ngModel)]="email" #emailModel="ngModel" [class.invalid]="emailModel.invalid && emailModel.touched" />
            @if (emailModel.invalid && emailModel.touched) {
              <span class="field-error">Enter a valid email address.</span>
            }
          </div>
          <div class="field">
            <label for="password">Password</label>
            <input id="password" class="input" name="password" type="password" autocomplete="current-password" required
                   [(ngModel)]="password" #passwordModel="ngModel" [class.invalid]="passwordModel.invalid && passwordModel.touched" />
          </div>
          <button class="btn block" type="submit" [disabled]="busy() || form.invalid">
            @if (busy()) { <span class="spinner"></span> Signing in… } @else { Sign in }
          </button>
        </form>

        <p class="auth-footer">New to DevFlow? <a routerLink="/register">Create an account</a></p>
      </div>
    </section>
  `,
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  /** Bound from the query string via withComponentInputBinding. */
  readonly returnUrl = input<string | undefined>();
  readonly reason = input<string | undefined>();

  email = '';
  password = '';
  readonly busy = signal(false);
  readonly errors = signal<FormErrors>({ message: '', fields: {} });

  submit(): void {
    this.busy.set(true);
    this.errors.set({ message: '', fields: {} });

    this.auth.login(this.email.trim(), this.password).subscribe({
      next: () => void this.router.navigateByUrl(safeReturnUrl(this.returnUrl())),
      error: error => {
        const errors = toFormErrors(error, 'Unable to sign in.');
        // Don't reveal whether the email exists or the account is locked.
        this.errors.set(
          (error as { status?: number }).status === 401
            ? { message: 'Invalid email or password. After 5 failed attempts the account is locked for 15 minutes.', fields: {} }
            : errors,
        );
        this.password = '';
        this.busy.set(false);
      },
    });
  }
}

/** Only allow in-app relative paths, never another origin. */
export function safeReturnUrl(url: string | undefined): string {
  return url && url.startsWith('/') && !url.startsWith('//') && !url.startsWith('/login') ? url : '/dashboard';
}
