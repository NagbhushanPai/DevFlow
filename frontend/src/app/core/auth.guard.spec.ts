import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { describe, expect, it } from 'vitest';
import { authGuard } from './auth.guard';
import { AuthService } from './auth.service';

describe('authGuard', () => {
  it('allows authenticated users', () => {
    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: { isAuthenticated: () => true } },
        provideRouter([])
      ]
    });

    expect(TestBed.runInInjectionContext(() => authGuard({} as never, {} as never))).toBe(true);
  });

  it('redirects anonymous users to login', () => {
    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: { isAuthenticated: () => false } },
        provideRouter([])
      ]
    });

    const result = TestBed.runInInjectionContext(() => authGuard({} as never, {} as never));

    expect(result).toEqual(TestBed.inject(Router).createUrlTree(['/login']));
  });
});
