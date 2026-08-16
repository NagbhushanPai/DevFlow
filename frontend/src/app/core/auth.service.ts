import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs';
import { AuthenticationResult } from './auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly key = 'devflow.auth';
  private readonly state = signal<AuthenticationResult | null>(this.read());
  readonly user = this.state.asReadonly();
  readonly isAuthenticated = () => { const value = this.state(); return !!value && new Date(value.expiresAtUtc) > new Date(); };
  constructor(private readonly http: HttpClient, private readonly router: Router) {}
  login(email: string, password: string) { return this.http.post<AuthenticationResult>('/api/auth/login', { email, password }).pipe(tap(result => this.save(result))); }
  save(result: AuthenticationResult) { localStorage.setItem(this.key, JSON.stringify(result)); this.state.set(result); }
  token() { return this.isAuthenticated() ? this.state()!.token : null; }
  logout() { localStorage.removeItem(this.key); this.state.set(null); void this.router.navigate(['/login']); }
  private read(): AuthenticationResult | null { try { return JSON.parse(localStorage.getItem(this.key) ?? 'null') as AuthenticationResult | null; } catch { return null; } }
}
