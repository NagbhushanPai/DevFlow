import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { ConfirmService } from '../../core/confirm.service';
import { GitHubSummary, OrganizationMember, PROJECT_ROLES, PROJECT_STATUSES, ProjectMember, ProjectRole, ProjectStatus, Team } from '../../core/models';
import { FormErrors, errorMessage, toFormErrors } from '../../core/problem';
import { ToastService } from '../../core/toast.service';
import { AvatarComponent } from '../../shared/avatar.component';
import { LabelPipe } from '../../shared/pipes';
import { ProjectContext } from './project-context';

@Component({
  selector: 'app-project-settings',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, AvatarComponent, LabelPipe],
  template: `
    <div class="stack" style="max-width: 860px">
      @if (canManage()) {
        <form class="card" (ngSubmit)="save()" #form="ngForm">
          <h2>General</h2>
          @if (errors().message) { <div class="alert error">{{ errors().message }}</div> }
          <div class="form-grid">
            <div class="field">
              <label for="name">Name</label>
              <input id="name" class="input" name="name" required maxlength="200" [(ngModel)]="draft.name" />
            </div>
            <div class="field">
              <label for="key">Key</label>
              <input id="key" class="input mono" name="key" required pattern="[A-Za-z0-9]{2,10}" maxlength="10" [(ngModel)]="draft.key" />
              <span class="hint">Changing the key changes every issue ID.</span>
              @for (message of errors().fields['key']; track message) { <span class="field-error">{{ message }}</span> }
            </div>
            <div class="field">
              <label for="status">Status</label>
              <select id="status" class="select" name="status" [(ngModel)]="draft.status">
                @for (option of statuses; track option.value) { <option [ngValue]="option.value">{{ option.label }}</option> }
              </select>
            </div>
          </div>
          <div class="field">
            <label for="description">Description</label>
            <textarea id="description" class="textarea" name="description" maxlength="2000" [(ngModel)]="draft.description"></textarea>
          </div>
          <div class="form-actions"><button class="btn" type="submit" [disabled]="busy() || form.invalid">Save changes</button></div>
        </form>
      }

      <div class="card flush">
        <div class="card-header" style="padding: 20px 20px 0"><h2>Members</h2><span class="subtle">{{ context.members().length }}</span></div>
        @if (canManage() && candidates().length) {
          <form class="row" style="padding: 0 20px 16px" (ngSubmit)="addMember()">
            <select class="select" style="flex: 1; min-width: 220px" name="candidate" aria-label="Organization member to add" [(ngModel)]="candidateId" required>
              <option value="">Add an organization member…</option>
              @for (member of candidates(); track member.userId) { <option [value]="member.userId">{{ member.fullName }} ({{ member.email }})</option> }
            </select>
            <select class="select" style="width: auto" name="candidateRole" aria-label="Role" [(ngModel)]="candidateRole">
              @for (option of roles; track option.value) { <option [ngValue]="option.value">{{ option.label }}</option> }
            </select>
            <button class="btn" type="submit" [disabled]="!candidateId">Add</button>
          </form>
        }
        <div class="table-wrap">
          <table class="table">
            <thead><tr><th>Member</th><th class="hide-mobile">Email</th><th>Role</th>@if (canManage()) {<th></th>}</tr></thead>
            <tbody>
              @for (member of context.members(); track member.userId) {
                <tr>
                  <td>
                    <app-avatar [name]="member.fullName" [seed]="member.userId" [showName]="true" size="sm" />
                    @if (member.userId === ownerId()) { <span class="badge" style="margin-left: 6px">Owner</span> }
                  </td>
                  <td class="muted hide-mobile">{{ member.email }}</td>
                  <td>
                    @if (canManage() && member.userId !== ownerId()) {
                      <select class="select" style="width: auto; min-height: 30px" [attr.aria-label]="'Role for ' + member.fullName"
                              [ngModel]="member.role" (ngModelChange)="setRole(member, $event)">
                        @for (option of roles; track option.value) { <option [ngValue]="option.value">{{ option.label }}</option> }
                      </select>
                    } @else {
                      <span class="badge role">{{ member.role | label: roles }}</span>
                    }
                  </td>
                  @if (canManage()) {
                    <td class="row end">
                      @if (member.userId !== ownerId()) {
                        <button class="btn danger-ghost sm" type="button" (click)="removeMember(member)">Remove</button>
                      }
                    </td>
                  }
                </tr>
              }
            </tbody>
          </table>
        </div>
        <p class="subtle" style="padding: 12px 20px; margin: 0; border-top: 1px solid var(--border)">
          Managers change settings and run sprints · Developers create and update issues · Viewers have read-only access.
        </p>
      </div>

      <div class="card">
        <div class="card-header"><h2>Teams</h2></div>
        <ul class="list">
          @for (team of teams(); track team.id) {
            <li>
              <strong>{{ team.name }}</strong>
              <span class="muted person-name">{{ team.description }}</span>
              <span class="spacer"></span>
              @if (canManage()) { <button class="btn danger-ghost sm" type="button" (click)="removeTeam(team)">Remove</button> }
            </li>
          } @empty {
            <li class="muted">No teams assigned.</li>
          }
        </ul>
        @if (canManage() && availableTeams().length) {
          <form class="row" style="margin-top: 12px" (ngSubmit)="assignTeam()">
            <select class="select" style="flex: 1" name="team" aria-label="Team to assign" [(ngModel)]="teamId" required>
              <option value="">Assign a team…</option>
              @for (team of availableTeams(); track team.id) { <option [value]="team.id">{{ team.name }}</option> }
            </select>
            <button class="btn secondary" type="submit" [disabled]="!teamId">Assign</button>
          </form>
        }
      </div>

      <div class="card">
        <div class="card-header"><h2>GitHub repository</h2></div>
        @if (gitHub(); as summary) {
          <div class="row" style="margin-bottom: 12px">
            <a [href]="summary.htmlUrl" target="_blank" rel="noopener noreferrer"><strong>{{ summary.owner }}/{{ summary.name }}</strong></a>
            <span class="badge">{{ summary.openIssuesCount }} open issues</span>
            <span class="badge mono">{{ summary.defaultBranch }}</span>
          </div>
          @if (summary.description) { <p class="muted">{{ summary.description }}</p> }
        } @else if (gitHubError()) {
          <div class="alert error">{{ gitHubError() }}</div>
        } @else if (!context.project()?.gitHubRepositoryOwner) {
          <p class="muted">No repository linked.</p>
        }
        @if (canManage()) {
          <form class="row" (ngSubmit)="saveGitHub()">
            <input class="input" style="flex: 1; min-width: 140px" name="owner" placeholder="owner" aria-label="Repository owner" [(ngModel)]="repoOwner" />
            <span class="muted">/</span>
            <input class="input" style="flex: 1; min-width: 140px" name="repo" placeholder="repository" aria-label="Repository name" [(ngModel)]="repoName" />
            <button class="btn secondary" type="submit">{{ repoOwner || repoName ? 'Link' : 'Unlink' }}</button>
          </form>
        }
      </div>

      @if (canManage()) {
        <div class="card" style="border-color: var(--danger)">
          <h2>Danger zone</h2>
          <p class="muted">Deleting the project permanently removes its issues, sprints, comments and attachments.</p>
          <button class="btn danger" type="button" (click)="deleteProject()">Delete project</button>
        </div>
      }
    </div>
  `,
})
export class ProjectSettingsComponent {
  readonly context = inject(ProjectContext);
  private readonly api = inject(ApiService);
  private readonly confirm = inject(ConfirmService);
  private readonly toasts = inject(ToastService);
  private readonly router = inject(Router);

  readonly statuses = PROJECT_STATUSES;
  readonly roles = PROJECT_ROLES;
  readonly canManage = computed(() => this.context.permissions().canManage);
  readonly ownerId = computed(() => this.context.project()?.ownerId);
  readonly teams = signal<Team[]>([]);
  readonly organizationTeams = signal<Team[]>([]);
  readonly organizationMembers = signal<OrganizationMember[]>([]);
  readonly gitHub = signal<GitHubSummary | null>(null);
  readonly gitHubError = signal('');
  readonly busy = signal(false);
  readonly errors = signal<FormErrors>({ message: '', fields: {} });

  readonly candidates = computed(() => {
    const existing = new Set(this.context.members().map(member => member.userId));
    return this.organizationMembers().filter(member => !existing.has(member.userId));
  });
  readonly availableTeams = computed(() => {
    const assigned = new Set(this.teams().map(team => team.id));
    return this.organizationTeams().filter(team => !assigned.has(team.id));
  });

  draft = { name: '', key: '', description: '', status: ProjectStatus.Active };
  candidateId = '';
  candidateRole = ProjectRole.Developer;
  teamId = '';
  repoOwner = '';
  repoName = '';

  constructor() {
    const project = this.context.project();
    if (!project) return;

    this.draft = { name: project.name, key: project.key, description: project.description ?? '', status: project.status };
    this.repoOwner = project.gitHubRepositoryOwner ?? '';
    this.repoName = project.gitHubRepositoryName ?? '';

    this.loadTeams();
    if (project.gitHubRepositoryOwner) this.loadGitHub();
    if (this.canManage()) {
      this.api.get<OrganizationMember[]>(`/organizations/${project.organizationId}/members`).subscribe(items => this.organizationMembers.set(items));
      this.api.get<Team[]>(`/organizations/${project.organizationId}/teams`).subscribe(items => this.organizationTeams.set(items));
    }
  }

  save(): void {
    const project = this.context.project();
    if (!project) return;
    const body = { name: this.draft.name.trim(), key: this.draft.key.trim().toUpperCase(), description: this.draft.description.trim() || null, status: this.draft.status };

    this.busy.set(true);
    this.errors.set({ message: '', fields: {} });
    this.api.put<void>(`/projects/${project.id}`, body).subscribe({
      next: () => {
        this.context.project.set({ ...project, ...body });
        this.busy.set(false);
        this.toasts.success('Project updated.');
      },
      error: error => {
        this.errors.set(toFormErrors(error));
        this.busy.set(false);
      },
    });
  }

  addMember(): void {
    this.setRole({ userId: this.candidateId } as ProjectMember, this.candidateRole, () => (this.candidateId = ''));
  }

  setRole(member: ProjectMember, role: ProjectRole, done?: () => void): void {
    this.api.put<void>(`/projects/${this.projectId}/members/${member.userId}`, { role }).subscribe({
      next: () => {
        this.context.loadMembers();
        done?.();
      },
      error: error => {
        this.toasts.error(errorMessage(error));
        this.context.loadMembers();
      },
    });
  }

  async removeMember(member: ProjectMember): Promise<void> {
    if (!(await this.confirm.ask({ title: `Remove ${member.fullName}?`, message: 'They will lose access to this project.', confirmLabel: 'Remove', danger: true }))) return;
    this.api.delete<void>(`/projects/${this.projectId}/members/${member.userId}`).subscribe({
      next: () => this.context.loadMembers(),
      error: error => this.toasts.error(errorMessage(error)),
    });
  }

  assignTeam(): void {
    this.api.put<void>(`/projects/${this.projectId}/teams/${this.teamId}`).subscribe({
      next: () => {
        this.teamId = '';
        this.loadTeams();
      },
      error: error => this.toasts.error(errorMessage(error)),
    });
  }

  removeTeam(team: Team): void {
    this.api.delete<void>(`/projects/${this.projectId}/teams/${team.id}`).subscribe({
      next: () => this.loadTeams(),
      error: error => this.toasts.error(errorMessage(error)),
    });
  }

  saveGitHub(): void {
    const owner = this.repoOwner.trim() || null;
    const name = this.repoName.trim() || null;
    this.api.put<void>(`/projects/${this.projectId}/github`, { owner, name }).subscribe({
      next: () => {
        this.context.project.update(project => (project ? { ...project, gitHubRepositoryOwner: owner, gitHubRepositoryName: name } : project));
        this.gitHub.set(null);
        this.gitHubError.set('');
        if (owner) this.loadGitHub();
        this.toasts.success(owner ? 'Repository linked.' : 'Repository unlinked.');
      },
      error: error => this.toasts.error(errorMessage(error)),
    });
  }

  async deleteProject(): Promise<void> {
    const project = this.context.project();
    if (!project) return;
    const confirmed = await this.confirm.ask({
      title: `Delete ${project.key} · ${project.name}?`,
      message: 'This permanently deletes the project and everything in it. This cannot be undone.',
      confirmLabel: 'Delete project',
      danger: true,
    });
    if (!confirmed) return;

    this.api.delete<void>(`/projects/${project.id}`).subscribe({
      next: () => {
        this.toasts.success('Project deleted.');
        void this.router.navigate(['/projects']);
      },
      error: error => this.toasts.error(errorMessage(error)),
    });
  }

  private get projectId(): string {
    return this.context.project()?.id ?? '';
  }

  private loadTeams(): void {
    this.api.get<Team[]>(`/projects/${this.projectId}/teams`).subscribe(items => this.teams.set(items));
  }

  private loadGitHub(): void {
    this.api.get<GitHubSummary | null>(`/projects/${this.projectId}/github/summary`).subscribe({
      next: summary => this.gitHub.set(summary),
      error: () => this.gitHubError.set('GitHub is unavailable right now, or the repository does not exist.'),
    });
  }
}
