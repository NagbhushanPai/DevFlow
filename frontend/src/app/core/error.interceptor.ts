import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { errorMessage } from './problem';
import { ToastService } from './toast.service';

/**
 * Surfaces errors that a component usually can't handle itself (network down, server errors, forbidden,
 * rate limited) as toasts. 400/404/409 are left to the calling component, which shows them in context.
 */
export const errorInterceptor: HttpInterceptorFn = (request, next) => {
  const toasts = inject(ToastService);

  return next(request).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && !request.url.startsWith('/api/auth/')) {
        if (error.status === 0) {
          toasts.error('Cannot reach the DevFlow server. Check your connection.');
        } else if (error.status === 403) {
          toasts.error(errorMessage(error, 'You do not have permission to do that.'));
        } else if (error.status === 429) {
          toasts.error('Too many requests. Please slow down.');
        } else if (error.status >= 500) {
          toasts.error(errorMessage(error, 'The server ran into a problem. Please try again.'));
        }
      }

      return throwError(() => error);
    }),
  );
};
