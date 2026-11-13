import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { FormErrors, toFormErrors } from '../../core/problem';

export interface PasswordRule { label: string; test: (value: string) => boolean; }

/** Mirrors the API's password policy (RegisterCommandValidator + Identity options). */
export const PASSWORD_RULES: PasswordRule[] = [
  { label: '8+ characters', test: value => value.length >= 8 },
  { label: 'Uppercase letter', test: value => /[A-Z]/.test(value) },
  { label: 'Lowercase letter', test: value => /[a-z]/.test(value) },
  { label: 'Number', test: value => /[0-9]/.test(value) },
  { label: 'Symbol', test: value => /[^a-zA-Z0-9]/.test(value) },
];

@Component({
  selector: 'app-register',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, RouterLink],
  template: `
    <section class="auth-page">
      <div class="auth-card">
        <h1>Create your account</h1>
        <p>Start planning and tracking work with your team.</p>

        @if (errors().message) {
          <div class="alert error" role="alert">{{ errors().message }}</div>
        }

        <form (ngSubmit)="submit()" #form="ngForm" novalidate>
          <div class="form-grid">
            <div class="field">
              <label for="firstName">First name</label>
              <input id="firstName" class="input" name="firstName" autocomplete="given-name" required maxlength="100" [(ngModel)]="firstName" />
              @for (message of errors().fields['firstName']; track message) { <span class="field-error">{{ message }}</span> }
            </div>
            <div class="field">
              <label for="lastName">Last name</label>
              <input id="lastName" class="input" name="lastName" autocomplete="family-name" required maxlength="100" [(ngModel)]="lastName" />
              @for (message of errors().fields['lastName']; track message) { <span class="field-error">{{ message }}</span> }
            </div>
          </div>
          <div class="field">
            <label for="email">Email</label>
            <input id="email" class="input" name="email" type="email" autocomplete="email" required email [(ngModel)]="email" />
            @for (message of errors().fields['email']; track message) { <span class="field-error">{{ message }}</span> }
          </div>
          <div class="field">
            <label for="password">Password</label>
            <input id="password" class="input" name="password" type="password" autocomplete="new-password" required
                   [ngModel]="password()" (ngModelChange)="password.set($event)" />
            <ul class="password-rules" aria-label="Password requirements">
              @for (rule of rules; track rule.label) {
                <li [class.ok]="rule.test(password())">{{ rule.label }}</li>
              }
            </ul>
            @for (message of errors().fields['password']; track message) { <span class="field-error">{{ message }}</span> }
          </div>
          <button class="btn block" type="submit" [disabled]="busy() || form.invalid || !passwordValid()">
            @if (busy()) { <span class="spinner"></span> Creating account… } @else { Create account }
          </button>
        </form>

        <p class="auth-footer">Already have an account? <a routerLink="/login">Sign in</a></p>
      </div>
    </section>
  `,
})
export class RegisterComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly rules = PASSWORD_RULES;
  firstName = '';
  lastName = '';
  email = '';
  readonly password = signal('');
  readonly passwordValid = computed(() => this.rules.every(rule => rule.test(this.password())));
  readonly busy = signal(false);
  readonly errors = signal<FormErrors>({ message: '', fields: {} });

  submit(): void {
    this.busy.set(true);
    this.errors.set({ message: '', fields: {} });

    this.auth.register(this.firstName.trim(), this.lastName.trim(), this.email.trim(), this.password()).subscribe({
      next: () => void this.router.navigate(['/dashboard']),
      error: error => {
        this.errors.set(toFormErrors(error, 'Unable to create the account.'));
        this.busy.set(false);
      },
    });
  }
}
