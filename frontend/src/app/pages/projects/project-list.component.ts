import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Subject, debounceTime } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { Organization, PROJECT_ROLES, PROJECT_STATUSES, Page, Project, ProjectStatus } from '../../core/models';
import { canManageOrganization } from '../../core/permissions';
import { FormErrors, toFormErrors } from '../../core/problem';
import { ToastService } from '../../core/toast.service';
import { LabelPipe } from '../../shared/pipes';

@Component({
  selector: 'app-project-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, RouterLink, LabelPipe],
  template: `
    <section class="page">
      <header class="page-header">
        <div>
          <p class="eyebrow">Delivery</p>
          <h1>Projects</h1>
          <p>Projects you belong to, or that your organizations own.</p>
        </div>
        @if (manageableOrganizations().length) {
          <button class="btn" type="button" (click)="openCreate()">+ New project</button>
        }
      </header>

      <div class="toolbar">
        <input class="input search" type="search" placeholder="Search by name or key…" aria-label="Search projects"
               [ngModel]="search()" (ngModelChange)="search.set($event); searchChanges.next()" />
        <select class="select" aria-label="Filter by status" [ngModel]="status()" (ngModelChange)="status.set($event); load()">
          <option [ngValue]="null">All statuses</option>
          @for (option of statuses; track option.value) { <option [ngValue]="option.value">{{ option.label }}</option> }
        </select>
      </div>

      @if (loading()) {
        <div class="grid"><div class="skeleton block"></div><div class="skeleton block"></div><div class="skeleton block"></div></div>
      } @else {
        <div class="grid">
          @for (project of projects(); track project.id) {
            <a class="card link-card" [routerLink]="['/projects', project.id]">
              <div class="row">
                <span class="badge mono">{{ project.key }}</span>
                <span class="badge" [class.sprint-2]="project.status === active">{{ project.status | label: statuses }}</span>
                <span class="spacer"></span>
                @if (project.myRole) { <span class="badge role">{{ project.myRole | label: roles }}</span> }
              </div>
              <h2 style="margin-top: 12px">{{ project.name }}</h2>
              <p class="muted">{{ project.description || 'No description' }}</p>
              <span class="subtle">{{ organizationName(project.organizationId) }}</span>
            </a>
          } @empty {
            <div class="card empty" style="grid-column: 1 / -1">
              @if (search() || status()) {
                <h3>No matching projects</h3>
                <p>Try a different search or status filter.</p>
              } @else if (organizations().length === 0) {
                <h3>No projects yet</h3>
                <p>Projects live inside an organization. Create an organization first.</p>
                <a class="btn" routerLink="/organizations">Go to organizations</a>
              } @else if (manageableOrganizations().length) {
                <h3>No projects yet</h3>
                <p>Create the first project for your team.</p>
                <button class="btn" type="button" (click)="openCreate()">Create a project</button>
              } @else {
                <h3>No projects yet</h3>
                <p>Ask an organization owner or admin to add you to a project.</p>
              }
            </div>
          }
        </div>
      }
    </section>

    @if (showCreate()) {
      <div class="backdrop" (click)="showCreate.set(false)">
        <form class="dialog" (click)="$event.stopPropagation()" (ngSubmit)="create()" #form="ngForm" aria-label="New project">
          <h2>New project</h2>
          @if (errors().message) { <div class="alert error">{{ errors().message }}</div> }
          <div class="field">
            <label for="project-org">Organization</label>
            <select id="project-org" class="select" name="organizationId" required [(ngModel)]="draft.organizationId">
              @for (organization of manageableOrganizations(); track organization.id) {
                <option [value]="organization.id">{{ organization.name }}</option>
              }
            </select>
          </div>
          <div class="form-grid">
            <div class="field">
              <label for="project-name">Name</label>
              <input id="project-name" class="input" name="name" required maxlength="200" placeholder="Mobile app"
                     [(ngModel)]="draft.name" (ngModelChange)="suggestKey($event)" />
              @for (message of errors().fields['name']; track message) { <span class="field-error">{{ message }}</span> }
            </div>
            <div class="field">
              <label for="project-key">Key</label>
              <input id="project-key" class="input mono" name="key" required pattern="[A-Za-z0-9]{2,10}" maxlength="10" placeholder="MOB"
                     [(ngModel)]="draft.key" (ngModelChange)="keyEdited = true" />
              <span class="hint">2–10 letters or digits, used in issue IDs.</span>
              @for (message of errors().fields['key']; track message) { <span class="field-error">{{ message }}</span> }
            </div>
          </div>
          <div class="field">
            <label for="project-description">Description <span class="subtle">(optional)</span></label>
            <textarea id="project-description" class="textarea" name="description" maxlength="2000" [(ngModel)]="draft.description"></textarea>
          </div>
          <div class="form-actions">
            <button class="btn secondary" type="button" (click)="showCreate.set(false)">Cancel</button>
            <button class="btn" type="submit" [disabled]="busy() || form.invalid">Create project</button>
          </div>
        </form>
      </div>
    }
  `,
})
export class ProjectListComponent {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  private readonly toasts = inject(ToastService);

  readonly statuses = PROJECT_STATUSES;
  readonly roles = PROJECT_ROLES;
  readonly active = ProjectStatus.Active;
  readonly projects = signal<Project[]>([]);
  readonly organizations = signal<Organization[]>([]);
  readonly manageableOrganizations = computed(() => this.organizations().filter(org => canManageOrganization(org.myRole)));
  readonly loading = signal(true);
  readonly search = signal('');
  readonly status = signal<ProjectStatus | null>(null);
  readonly searchChanges = new Subject<void>();
  readonly showCreate = signal(false);
  readonly busy = signal(false);
  readonly errors = signal<FormErrors>({ message: '', fields: {} });
  draft = { organizationId: '', name: '', key: '', description: '' };
  keyEdited = false;

  constructor() {
    this.searchChanges.pipe(debounceTime(250)).subscribe(() => this.load());
    this.api.get<Organization[]>('/organizations').subscribe(items => this.organizations.set(items));
    this.load();
  }

  load(): void {
    this.api.get<Page<Project>>('/projects', { search: this.search().trim(), status: this.status(), pageSize: 100 }).subscribe({
      next: page => {
        this.projects.set(page.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  organizationName(id: string): string {
    return this.organizations().find(org => org.id === id)?.name ?? '';
  }

  openCreate(): void {
    this.draft = { organizationId: this.manageableOrganizations()[0]?.id ?? '', name: '', key: '', description: '' };
    this.keyEdited = false;
    this.errors.set({ message: '', fields: {} });
    this.showCreate.set(true);
  }

  /** Suggest a key from the name ("Mobile App" → "MA", "Payments" → "PAY") until the user edits it. */
  suggestKey(name: string): void {
    if (this.keyEdited) return;
    const words = name.toUpperCase().replace(/[^A-Z0-9 ]/g, '').split(' ').filter(Boolean);
    this.draft.key = (words.length > 1 ? words.map(word => word[0]).join('') : (words[0] ?? '').slice(0, 3)).slice(0, 10);
  }

  create(): void {
    this.busy.set(true);
    const body = { ...this.draft, name: this.draft.name.trim(), key: this.draft.key.trim().toUpperCase(), description: this.draft.description.trim() || null };
    this.api.post<string>('/projects', body).subscribe({
      next: id => {
        this.toasts.success(`Project ${body.key} created.`);
        void this.router.navigate(['/projects', id]);
      },
      error: error => {
        this.errors.set(toFormErrors(error));
        this.busy.set(false);
      },
    });
  }
}
