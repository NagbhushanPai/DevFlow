import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';

describe('authInterceptor', () => {
  let http: HttpClient;
  let controller: HttpTestingController;
  let token: string | null;
  let refresh: ReturnType<typeof vi.fn<() => Observable<{ token: string }>>>;
  const navigate = vi.fn().mockResolvedValue(true);

  beforeEach(() => {
    token = 'access-1';
    refresh = vi.fn(() => of({ token: 'access-2' }));
    navigate.mockClear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: { token: () => token, refresh } },
        { provide: Router, useValue: { navigate, url: '/projects/1' } },
      ],
    });
    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
  });

  it('adds the bearer token to API calls', () => {
    http.get('/api/projects').subscribe();
    expect(controller.expectOne('/api/projects').request.headers.get('Authorization')).toBe('Bearer access-1');
  });

  it('does not add tokens to auth endpoints or non-API URLs', () => {
    http.post('/api/auth/login', {}).subscribe();
    http.get('/assets/logo.svg').subscribe();
    expect(controller.expectOne('/api/auth/login').request.headers.has('Authorization')).toBe(false);
    expect(controller.expectOne('/assets/logo.svg').request.headers.has('Authorization')).toBe(false);
  });

  it('refreshes once on 401 and retries with the new token', () => {
    let body: unknown;
    http.get('/api/projects').subscribe(result => (body = result));

    controller.expectOne('/api/projects').flush(null, { status: 401, statusText: 'Unauthorized' });
    const retry = controller.expectOne('/api/projects');
    expect(retry.request.headers.get('Authorization')).toBe('Bearer access-2');
    retry.flush({ ok: true });

    expect(refresh).toHaveBeenCalledTimes(1);
    expect(body).toEqual({ ok: true });
  });

  it('sends the user to login when the refresh fails', () => {
    refresh.mockReturnValue(throwError(() => new Error('expired')));
    let failed = false;
    http.get('/api/projects').subscribe({ error: () => (failed = true) });

    controller.expectOne('/api/projects').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(failed).toBe(true);
    expect(navigate).toHaveBeenCalledWith(['/login'], { queryParams: { returnUrl: '/projects/1', reason: 'expired' } });
  });

  it('passes through non-401 errors without refreshing', () => {
    let status = 0;
    http.get('/api/projects').subscribe({ error: error => (status = error.status) });
    controller.expectOne('/api/projects').flush(null, { status: 403, statusText: 'Forbidden' });

    expect(status).toBe(403);
    expect(refresh).not.toHaveBeenCalled();
  });
});
