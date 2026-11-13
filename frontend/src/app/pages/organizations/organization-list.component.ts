import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { Organization } from '../../core/models';
import { FormErrors, toFormErrors } from '../../core/problem';
import { ToastService } from '../../core/toast.service';

@Component({
  selector: 'app-organization-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, RouterLink],
  template: `
    <section class="page">
      <header class="page-header">
        <div>
          <p class="eyebrow">Workspace</p>
          <h1>Organizations</h1>
          <p>Organizations group your people, teams and projects.</p>
        </div>
        <button class="btn" type="button" (click)="showCreate.set(true)">+ New organization</button>
      </header>

      @if (loading()) {
        <div class="grid"><div class="skeleton block"></div><div class="skeleton block"></div></div>
      } @else {
        <div class="grid">
          @for (organization of organizations(); track organization.id) {
            <a class="card link-card" [routerLink]="['/organizations', organization.id]">
              <div class="row between">
                <h2 style="margin: 0">{{ organization.name }}</h2>
                <span class="badge role">{{ organization.myRole }}</span>
              </div>
              <p class="muted" style="margin-top: 8px">{{ organization.description || 'No description' }}</p>
            </a>
          } @empty {
            <div class="card empty" style="grid-column: 1 / -1">
              <h3>No organizations yet</h3>
              <p>Create one to start adding teams and projects, or ask an owner to add you to theirs.</p>
              <button class="btn" type="button" (click)="showCreate.set(true)">Create an organization</button>
            </div>
          }
        </div>
      }
    </section>

    @if (showCreate()) {
      <div class="backdrop" (click)="close()">
        <form class="dialog" (click)="$event.stopPropagation()" (ngSubmit)="create()" #form="ngForm" aria-label="New organization">
          <h2>New organization</h2>
          @if (errors().message) { <div class="alert error">{{ errors().message }}</div> }
          <div class="field">
            <label for="org-name">Name</label>
            <input id="org-name" class="input" name="name" required maxlength="200" [(ngModel)]="name" autofocus />
          </div>
          <div class="field">
            <label for="org-description">Description <span class="subtle">(optional)</span></label>
            <textarea id="org-description" class="textarea" name="description" maxlength="1000" [(ngModel)]="description"></textarea>
          </div>
          <div class="form-actions">
            <button class="btn secondary" type="button" (click)="close()">Cancel</button>
            <button class="btn" type="submit" [disabled]="busy() || form.invalid">Create</button>
          </div>
        </form>
      </div>
    }
  `,
})
export class OrganizationListComponent {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  private readonly toasts = inject(ToastService);

  readonly organizations = signal<Organization[]>([]);
  readonly loading = signal(true);
  readonly showCreate = signal(false);
  readonly busy = signal(false);
  readonly errors = signal<FormErrors>({ message: '', fields: {} });
  name = '';
  description = '';

  constructor() {
    this.api.get<Organization[]>('/organizations').subscribe({
      next: items => {
        this.organizations.set(items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  create(): void {
    this.busy.set(true);
    this.api.post<string>('/organizations', { name: this.name.trim(), description: this.description.trim() || null }).subscribe({
      next: id => {
        this.toasts.success('Organization created.');
        void this.router.navigate(['/organizations', id]);
      },
      error: error => {
        this.errors.set(toFormErrors(error));
        this.busy.set(false);
      },
    });
  }

  close(): void {
    this.showCreate.set(false);
    this.errors.set({ message: '', fields: {} });
  }
}
