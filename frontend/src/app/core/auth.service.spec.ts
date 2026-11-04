import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthenticationResult } from './auth.models';
import { AuthService } from './auth.service';

const authentication: AuthenticationResult = {
  userId: 'user-id',
  firstName: 'Test',
  lastName: 'User',
  email: 'test@example.com',
  roles: ['Developer'],
  token: 'token',
  expiresAtUtc: '2099-01-01T00:00:00.000Z'
};

describe('AuthService', () => {
  beforeEach(() => localStorage.clear());

  it('logs in, stores the result, and exposes the token', () => {
    TestBed.configureTestingModule({
      providers: [AuthService, provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    });
    const service = TestBed.inject(AuthService);
    const http = TestBed.inject(HttpTestingController);

    service.login(authentication.email, 'password').subscribe();
    const request = http.expectOne('/api/auth/login');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ email: authentication.email, password: 'password' });
    request.flush(authentication);

    expect(service.isAuthenticated()).toBe(true);
    expect(service.token()).toBe(authentication.token);
    expect(JSON.parse(localStorage.getItem('devflow.auth')!)).toEqual(authentication);
    http.verify();
  });

  it('clears the session and navigates to login on logout', () => {
    const navigate = vi.fn().mockResolvedValue(true);
    TestBed.configureTestingModule({
      providers: [
        AuthService,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: Router, useValue: { navigate } }
      ]
    });
    const service = TestBed.inject(AuthService);
    service.save(authentication);

    service.logout();

    expect(localStorage.getItem('devflow.auth')).toBeNull();
    expect(service.isAuthenticated()).toBe(false);
    expect(navigate).toHaveBeenCalledWith(['/login']);
  });
});
