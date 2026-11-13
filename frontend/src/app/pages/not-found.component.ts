import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-not-found',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  template: `
    <section class="page page-narrow">
      <div class="card empty">
        <h1>Page not found</h1>
        <p>The page you're looking for doesn't exist or you don't have access to it.</p>
        <a class="btn" routerLink="/dashboard">Back to dashboard</a>
      </div>
    </section>
  `,
})
export class NotFoundComponent {}
