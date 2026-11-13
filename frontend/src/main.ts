import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { inject, provideAppInitializer, provideZoneChangeDetection } from '@angular/core';
import { bootstrapApplication } from '@angular/platform-browser';
import { provideRouter, withComponentInputBinding, withInMemoryScrolling } from '@angular/router';
import { AppComponent } from './app/app.component';
import { routes } from './app/app.routes';
import { authInterceptor } from './app/core/auth.interceptor';
import { AuthService } from './app/core/auth.service';
import { errorInterceptor } from './app/core/error.interceptor';

bootstrapApplication(AppComponent, {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes, withComponentInputBinding(), withInMemoryScrolling({ scrollPositionRestoration: 'top' })),
    // errorInterceptor sits outside authInterceptor so it only sees errors that survive the refresh-and-retry.
    provideHttpClient(withInterceptors([errorInterceptor, authInterceptor])),
    // Restore the session from the HttpOnly refresh cookie before the first route is resolved.
    provideAppInitializer(() => inject(AuthService).initialize()),
  ],
}).catch(error => console.error(error));
