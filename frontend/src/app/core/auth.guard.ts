import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

/** Requires a signed-in user; otherwise redirects to /login and remembers where the user was going. */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  if (auth.isAuthenticated()) {
    return true;
  }

  const returnUrl = state?.url && state.url !== '/' ? state.url : undefined;
  return inject(Router).createUrlTree(['/login'], returnUrl ? { queryParams: { returnUrl } } : {});
};

/** Keeps signed-in users away from the login/register pages. */
export const guestGuard: CanActivateFn = () =>
  inject(AuthService).isAuthenticated() ? inject(Router).createUrlTree(['/dashboard']) : true;
