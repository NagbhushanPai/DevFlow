import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthService } from './auth.service';
import { AuthResponse } from './models';

const session = (overrides: Partial<AuthResponse> = {}): AuthResponse => ({
  userId: 'user-1',
  firstName: 'Ada',
  lastName: 'Admin',
  email: 'ada@example.com',
  roles: ['Developer'],
  token: 'access-1',
  expiresAtUtc: new Date(Date.now() + 15 * 60_000).toISOString(),
  ...overrides,
});

describe('AuthService', () => {
  let service: AuthService;
  let http: HttpTestingController;
  const navigate = vi.fn().mockResolvedValue(true);

  beforeEach(() => {
    localStorage.clear();
    navigate.mockClear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), { provide: Router, useValue: { navigate } }],
    });
    service = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    service.clear();
    http.verify();
  });

  it('keeps the access token in memory only (never in localStorage)', () => {
    service.login('ada@example.com', 'Secret@123').subscribe();
    const request = http.expectOne('/api/auth/login');
    expect(request.request.body).toEqual({ email: 'ada@example.com', password: 'Secret@123' });
    request.flush(session());

    expect(service.isAuthenticated()).toBe(true);
    expect(service.token()).toBe('access-1');
    expect(service.displayName()).toBe('Ada Admin');
    expect(localStorage.length).toBe(0);
    expect(sessionStorage.length).toBe(0);
  });

  it('restores the session from the refresh cookie on start-up', async () => {
    const ready = service.initialize();
    http.expectOne(req => req.url === '/api/auth/refresh' && req.method === 'POST').flush(session({ token: 'restored' }));
    await ready;

    expect(service.isAuthenticated()).toBe(true);
    expect(service.token()).toBe('restored');
  });

  it('starts signed out when there is no valid refresh cookie', async () => {
    const ready = service.initialize();
    http.expectOne('/api/auth/refresh').flush(null, { status: 401, statusText: 'Unauthorized' });
    await ready;

    expect(service.isAuthenticated()).toBe(false);
  });

  it('shares one refresh request between concurrent callers', () => {
    const results: string[] = [];
    service.refresh().subscribe(r => results.push(r.token));
    service.refresh().subscribe(r => results.push(r.token));

    http.expectOne('/api/auth/refresh').flush(session({ token: 'shared' }));

    expect(results).toEqual(['shared', 'shared']);
  });

  it('logout revokes the refresh token on the server, clears the session and goes to /login', () => {
    service.login('ada@example.com', 'x').subscribe();
    http.expectOne('/api/auth/login').flush(session());

    service.logout();
    http.expectOne(req => req.url === '/api/auth/logout' && req.method === 'POST').flush(null);

    expect(service.isAuthenticated()).toBe(false);
    expect(navigate).toHaveBeenCalledWith(['/login']);
  });

  it('freshToken refreshes when the token is about to expire', async () => {
    service.login('ada@example.com', 'x').subscribe();
    http.expectOne('/api/auth/login').flush(session({ token: 'old', expiresAtUtc: new Date(Date.now() + 10_000).toISOString() }));

    const token = service.freshToken();
    http.expectOne('/api/auth/refresh').flush(session({ token: 'new' }));

    expect(await token).toBe('new');
  });

  it('exposes the Admin role', () => {
    service.login('ada@example.com', 'x').subscribe();
    http.expectOne('/api/auth/login').flush(session({ roles: ['Admin', 'Manager'] }));
    expect(service.isAdmin()).toBe(true);
  });
});
