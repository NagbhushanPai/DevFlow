import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';

const isAuthEndpoint = (url: string) => url.startsWith('/api/auth/');

const withToken = (request: HttpRequest<unknown>, token: string | null) =>
  token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request;

/**
 * Adds the bearer token to API calls. If a call fails with 401 (token expired), refreshes once via the
 * HttpOnly cookie and retries; if that fails too, the user is sent to the login page.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  if (!request.url.startsWith('/api/') || isAuthEndpoint(request.url)) {
    return next(request);
  }

  const auth = inject(AuthService);
  const router = inject(Router);

  return next(withToken(request, auth.token())).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401) {
        return throwError(() => error);
      }

      return auth.refresh().pipe(
        switchMap(session => next(withToken(request, session.token))),
        catchError(refreshError => {
          void router.navigate(['/login'], { queryParams: { returnUrl: router.url, reason: 'expired' } });
          return throwError(() => refreshError);
        }),
      );
    }),
  );
};
