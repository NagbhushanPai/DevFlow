import { HttpClient } from '@angular/common/http';
import { Injectable, OnDestroy, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, finalize, firstValueFrom, map, of, shareReplay, tap, throwError } from 'rxjs';
import { AuthResponse } from './models';

/** Refresh this long before the access token expires. */
const REFRESH_MARGIN_MS = 60_000;

/**
 * Session handling.
 *
 * The access token lives only in memory (never in localStorage), so injected scripts cannot steal a long-lived
 * credential. The refresh token is an HttpOnly cookie set by the API; on page load and shortly before expiry the
 * service calls /api/auth/refresh to obtain a new access token.
 */
@Injectable({ providedIn: 'root' })
export class AuthService implements OnDestroy {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly session = signal<AuthResponse | null>(null);
  private refreshInFlight?: Observable<AuthResponse>;
  private refreshTimer?: ReturnType<typeof setTimeout>;

  readonly user = this.session.asReadonly();
  readonly isAuthenticated = computed(() => this.session() !== null);
  readonly isAdmin = computed(() => this.session()?.roles.includes('Admin') ?? false);
  readonly displayName = computed(() => {
    const user = this.session();
    return user ? `${user.firstName} ${user.lastName}`.trim() : '';
  });

  /** Called once at start-up: restores the session from the refresh cookie if there is one. */
  initialize(): Promise<void> {
    return firstValueFrom(this.refresh().pipe(map(() => undefined), catchError(() => of(undefined))));
  }

  login(email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>('/api/auth/login', { email, password }).pipe(tap(result => this.setSession(result)));
  }

  register(firstName: string, lastName: string, email: string, password: string): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>('/api/auth/register', { firstName, lastName, email, password })
      .pipe(tap(result => this.setSession(result)));
  }

  /** Exchanges the refresh cookie for a new access token. Concurrent callers share one request. */
  refresh(): Observable<AuthResponse> {
    this.refreshInFlight ??= this.http.post<AuthResponse>('/api/auth/refresh', {}).pipe(
      tap(result => this.setSession(result)),
      catchError(error => {
        this.clear();
        return throwError(() => error);
      }),
      finalize(() => (this.refreshInFlight = undefined)),
      shareReplay(1),
    );
    return this.refreshInFlight;
  }

  /** Current access token, refreshing first if it is about to expire (used by SignalR). */
  async freshToken(): Promise<string> {
    const current = this.session();
    if (current && new Date(current.expiresAtUtc).getTime() - Date.now() > REFRESH_MARGIN_MS) {
      return current.token;
    }

    try {
      return (await firstValueFrom(this.refresh())).token;
    } catch {
      return '';
    }
  }

  token(): string | null {
    return this.session()?.token ?? null;
  }

  logout(): void {
    this.http.post('/api/auth/logout', {}).pipe(catchError(() => of(null))).subscribe();
    this.clear();
    void this.router.navigate(['/login']);
  }

  /** Drops the in-memory session without calling the API (e.g. after a failed refresh). */
  clear(): void {
    clearTimeout(this.refreshTimer);
    this.session.set(null);
  }

  ngOnDestroy(): void {
    clearTimeout(this.refreshTimer);
  }

  private setSession(result: AuthResponse): void {
    this.session.set(result);
    clearTimeout(this.refreshTimer);

    const delay = new Date(result.expiresAtUtc).getTime() - Date.now() - REFRESH_MARGIN_MS;
    this.refreshTimer = setTimeout(() => this.refresh().subscribe({ error: () => this.handleExpiredSession() }), Math.max(delay, 5_000));
  }

  private handleExpiredSession(): void {
    void this.router.navigate(['/login'], { queryParams: { reason: 'expired' } });
  }
}
