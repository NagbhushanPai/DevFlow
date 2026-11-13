import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { switchMap } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { ConfirmService } from '../../core/confirm.service';
import { Organization, OrganizationMember, OrganizationRole, PROJECT_STATUSES, Page, Project, Team, TeamMember, TeamRole, UserLookup } from '../../core/models';
import { canManageOrganization, isOrganizationOwner } from '../../core/permissions';
import { errorMessage } from '../../core/problem';
import { ToastService } from '../../core/toast.service';
import { AvatarComponent } from '../../shared/avatar.component';
import { LabelPipe } from '../../shared/pipes';

type Section = 'members' | 'teams' | 'projects' | 'settings';

@Component({
  selector: 'app-organization-detail',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, RouterLink, AvatarComponent, LabelPipe],
  template: `
    <section class="page">
      @if (organization(); as org) {
        <header class="page-header">
          <div>
            <nav class="breadcrumbs"><a routerLink="/organizations">Organizations</a> / <span>{{ org.name }}</span></nav>
            <h1>{{ org.name }}</h1>
            <p>{{ org.description || 'No description' }}</p>
          </div>
          <span class="badge role">Your role: {{ org.myRole }}</span>
        </header>

        <nav class="tabs" aria-label="Organization sections">
          @for (item of sections(); track item.id) {
            <a href="" [class.active]="section() === item.id" (click)="$event.preventDefault(); section.set(item.id)">{{ item.label }}</a>
          }
        </nav>

        @switch (section()) {
          @case ('members') {
            <div class="card flush">
              @if (isOwner()) {
                <form class="row" style="padding: 16px; border-bottom: 1px solid var(--border)" (ngSubmit)="addMember()">
                  <input class="input" style="flex: 1; min-width: 220px" name="email" type="email" placeholder="Add a member by email address"
                         [(ngModel)]="memberEmail" required />
                  <select class="select" style="width: auto" name="role" [(ngModel)]="memberRole">
                    <option value="Member">Member</option>
                    <option value="Admin">Admin</option>
                  </select>
                  <button class="btn" type="submit" [disabled]="!memberEmail.trim() || busy()">Add member</button>
                </form>
              }
              <div class="table-wrap">
                <table class="table">
                  <thead><tr><th>Member</th><th>Email</th><th>Role</th>@if (isOwner()) {<th></th>}</tr></thead>
                  <tbody>
                    @for (member of members(); track member.userId) {
                      <tr>
                        <td><app-avatar [name]="member.fullName" [seed]="member.userId" [showName]="true" size="sm" /></td>
                        <td class="muted">{{ member.email }}</td>
                        <td>
                          @if (isOwner() && member.role !== 'Owner') {
                            <select class="select" style="width: auto; min-height: 30px" [ngModel]="member.role" (ngModelChange)="changeRole(member, $event)" [attr.aria-label]="'Role for ' + member.fullName">
                              <option value="Member">Member</option>
                              <option value="Admin">Admin</option>
                            </select>
                          } @else {
                            <span class="badge role">{{ member.role }}</span>
                          }
                        </td>
                        @if (isOwner()) {
                          <td class="row end">
                            @if (member.role !== 'Owner') {
                              <button class="btn danger-ghost sm" type="button" (click)="removeMember(member)">Remove</button>
                            }
                          </td>
                        }
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
            </div>
          }

          @case ('teams') {
            <div class="stack">
              @if (canManage()) {
                <form class="card row" (ngSubmit)="createTeam()">
                  <input class="input" style="flex: 1; min-width: 200px" name="teamName" placeholder="New team name" maxlength="200" [(ngModel)]="teamName" required />
                  <input class="input" style="flex: 2; min-width: 200px" name="teamDescription" placeholder="Description (optional)" maxlength="1000" [(ngModel)]="teamDescription" />
                  <button class="btn" type="submit" [disabled]="!teamName.trim() || busy()">Create team</button>
                </form>
              }
              @for (team of teams(); track team.id) {
                <div class="card">
                  <div class="card-header">
                    <div>
                      <h3>{{ team.name }}</h3>
                      <span class="muted">{{ team.description || 'No description' }}</span>
                    </div>
                    <div class="row">
                      <button class="btn secondary sm" type="button" (click)="toggleTeam(team)">{{ expandedTeam() === team.id ? 'Hide members' : 'Members' }}</button>
                      @if (canManage()) {
                        <button class="btn danger-ghost sm" type="button" (click)="deleteTeam(team)">Delete</button>
                      }
                    </div>
                  </div>
                  @if (expandedTeam() === team.id) {
                    <ul class="list">
                      @for (member of teamMembers(); track member.userId) {
                        <li>
                          <app-avatar [name]="member.fullName" [seed]="member.userId" [showName]="true" size="sm" />
                          <span class="spacer"></span>
                          <span class="badge role">{{ member.role }}</span>
                          @if (canManage()) {
                            <button class="btn danger-ghost sm" type="button" (click)="removeTeamMember(team, member)">Remove</button>
                          }
                        </li>
                      } @empty {
                        <li class="muted">No members yet.</li>
                      }
                    </ul>
                    @if (canManage() && availableForTeam().length) {
                      <form class="row" style="margin-top: 12px" (ngSubmit)="addTeamMember(team)">
                        <select class="select" style="flex: 1; min-width: 200px" name="teamMemberId" [(ngModel)]="teamMemberId" required>
                          <option value="">Add an organization member…</option>
                          @for (member of availableForTeam(); track member.userId) {
                            <option [value]="member.userId">{{ member.fullName }} ({{ member.email }})</option>
                          }
                        </select>
                        <select class="select" style="width: auto" name="teamMemberRole" [(ngModel)]="teamMemberRole">
                          <option value="Developer">Developer</option>
                          <option value="Lead">Lead</option>
                          <option value="Viewer">Viewer</option>
                        </select>
                        <button class="btn secondary" type="submit" [disabled]="!teamMemberId">Add</button>
                      </form>
                    }
                  }
                </div>
              } @empty {
                <div class="card empty"><p>No teams yet.</p></div>
              }
            </div>
          }

          @case ('projects') {
            <div class="grid">
              @for (project of projects(); track project.id) {
                <a class="card link-card" [routerLink]="['/projects', project.id]">
                  <div class="row"><span class="badge mono">{{ project.key }}</span><span class="badge">{{ project.status | label: projectStatuses }}</span></div>
                  <h3 style="margin-top: 10px">{{ project.name }}</h3>
                  <p class="muted">{{ project.description || 'No description' }}</p>
                </a>
              } @empty {
                <div class="card empty" style="grid-column: 1 / -1">
                  <p>No projects you can see in this organization.</p>
                  @if (canManage()) { <a class="btn" routerLink="/projects">Create a project</a> }
                </div>
              }
            </div>
          }

          @case ('settings') {
            <div class="stack page-narrow" style="margin: 0">
              <form class="card" (ngSubmit)="save()" #form="ngForm">
                <h2>General</h2>
                <div class="field">
                  <label for="name">Name</label>
                  <input id="name" class="input" name="name" required maxlength="200" [(ngModel)]="editName" />
                </div>
                <div class="field">
                  <label for="description">Description</label>
                  <textarea id="description" class="textarea" name="description" maxlength="1000" [(ngModel)]="editDescription"></textarea>
                </div>
                <div class="form-actions"><button class="btn" type="submit" [disabled]="busy() || form.invalid">Save changes</button></div>
              </form>
              @if (isOwner()) {
                <div class="card" style="border-color: var(--danger)">
                  <h2>Danger zone</h2>
                  <p class="muted">Deleting the organization permanently removes its teams, projects and issues.</p>
                  <button class="btn danger" type="button" (click)="deleteOrganization(org)">Delete organization</button>
                </div>
              }
            </div>
          }
        }
      } @else if (loadError()) {
        <div class="card empty"><h3>Organization unavailable</h3><p>{{ loadError() }}</p><a class="btn" routerLink="/organizations">Back</a></div>
      } @else {
        <div class="skeleton" style="width: 40%; height: 28px; margin-bottom: 16px"></div>
        <div class="skeleton block"></div>
      }
    </section>
  `,
})
export class OrganizationDetailComponent {
  private readonly api = inject(ApiService);
  private readonly confirm = inject(ConfirmService);
  private readonly toasts = inject(ToastService);
  private readonly router = inject(Router);

  readonly organizationId = input.required<string>();

  readonly projectStatuses = PROJECT_STATUSES;
  readonly organization = signal<Organization | null>(null);
  readonly loadError = signal('');
  readonly members = signal<OrganizationMember[]>([]);
  readonly teams = signal<Team[]>([]);
  readonly projects = signal<Project[]>([]);
  readonly teamMembers = signal<TeamMember[]>([]);
  readonly expandedTeam = signal<string | null>(null);
  readonly section = signal<Section>('members');
  readonly busy = signal(false);

  readonly canManage = computed(() => canManageOrganization(this.organization()?.myRole));
  readonly isOwner = computed(() => isOrganizationOwner(this.organization()?.myRole));
  readonly sections = computed(() => {
    const items: { id: Section; label: string }[] = [
      { id: 'members', label: `Members (${this.members().length})` },
      { id: 'teams', label: `Teams (${this.teams().length})` },
      { id: 'projects', label: 'Projects' },
    ];
    if (this.canManage()) items.push({ id: 'settings', label: 'Settings' });
    return items;
  });
  readonly availableForTeam = computed(() => {
    const inTeam = new Set(this.teamMembers().map(member => member.userId));
    return this.members().filter(member => !inTeam.has(member.userId));
  });

  memberEmail = '';
  memberRole: OrganizationRole = 'Member';
  teamName = '';
  teamDescription = '';
  teamMemberId = '';
  teamMemberRole: TeamRole = 'Developer';
  editName = '';
  editDescription = '';

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    const id = this.organizationId();
    this.api.get<Organization>(`/organizations/${id}`).subscribe({
      next: organization => {
        this.organization.set(organization);
        this.editName = organization.name;
        this.editDescription = organization.description ?? '';
      },
      error: error => this.loadError.set(errorMessage(error, 'You may not be a member of this organization.')),
    });
    this.loadMembers();
    this.loadTeams();
    this.api.get<Page<Project>>('/projects', { pageSize: 100 }).subscribe(page =>
      this.projects.set(page.items.filter(project => project.organizationId === id)));
  }

  loadMembers(): void {
    this.api.get<OrganizationMember[]>(`/organizations/${this.organizationId()}/members`).subscribe(items => this.members.set(items));
  }

  loadTeams(): void {
    this.api.get<Team[]>(`/organizations/${this.organizationId()}/teams`).subscribe(items => this.teams.set(items));
  }

  addMember(): void {
    const email = this.memberEmail.trim();
    this.busy.set(true);
    this.api.get<UserLookup>('/users/lookup', { email }).pipe(
      switchMap(user => this.api.put<void>(`/organizations/${this.organizationId()}/members/${user.id}`, { role: this.memberRole })),
    ).subscribe({
      next: () => {
        this.toasts.success(`${email} added to the organization.`);
        this.memberEmail = '';
        this.busy.set(false);
        this.loadMembers();
      },
      error: error => {
        this.toasts.error(errorMessage(error, 'Could not add that member.'));
        this.busy.set(false);
      },
    });
  }

  changeRole(member: OrganizationMember, role: OrganizationRole): void {
    this.api.put<void>(`/organizations/${this.organizationId()}/members/${member.userId}`, { role }).subscribe({
      next: () => {
        this.toasts.success(`${member.fullName} is now ${role === 'Admin' ? 'an Admin' : 'a Member'}.`);
        this.loadMembers();
      },
      error: error => {
        this.toasts.error(errorMessage(error));
        this.loadMembers();
      },
    });
  }

  async removeMember(member: OrganizationMember): Promise<void> {
    if (!(await this.confirm.ask({ title: 'Remove member?', message: `${member.fullName} will lose access to this organization.`, confirmLabel: 'Remove', danger: true }))) return;
    this.api.delete<void>(`/organizations/${this.organizationId()}/members/${member.userId}`).subscribe({
      next: () => this.loadMembers(),
      error: error => this.toasts.error(errorMessage(error)),
    });
  }

  createTeam(): void {
    this.busy.set(true);
    this.api.post<Team>(`/organizations/${this.organizationId()}/teams`, { name: this.teamName.trim(), description: this.teamDescription.trim() || null }).subscribe({
      next: () => {
        this.teamName = '';
        this.teamDescription = '';
        this.busy.set(false);
        this.loadTeams();
      },
      error: error => {
        this.toasts.error(errorMessage(error));
        this.busy.set(false);
      },
    });
  }

  async deleteTeam(team: Team): Promise<void> {
    if (!(await this.confirm.ask({ title: `Delete ${team.name}?`, message: 'The team will be removed from all projects.', confirmLabel: 'Delete team', danger: true }))) return;
    this.api.delete<void>(`/teams/${team.id}`).subscribe({ next: () => this.loadTeams(), error: error => this.toasts.error(errorMessage(error)) });
  }

  toggleTeam(team: Team): void {
    if (this.expandedTeam() === team.id) {
      this.expandedTeam.set(null);
      return;
    }

    this.expandedTeam.set(team.id);
    this.teamMembers.set([]);
    this.loadTeamMembers(team);
  }

  addTeamMember(team: Team): void {
    this.api.put<void>(`/teams/${team.id}/members/${this.teamMemberId}`, { role: this.teamMemberRole }).subscribe({
      next: () => {
        this.teamMemberId = '';
        this.loadTeamMembers(team);
      },
      error: error => this.toasts.error(errorMessage(error)),
    });
  }

  removeTeamMember(team: Team, member: TeamMember): void {
    this.api.delete<void>(`/teams/${team.id}/members/${member.userId}`).subscribe({
      next: () => this.loadTeamMembers(team),
      error: error => this.toasts.error(errorMessage(error)),
    });
  }

  save(): void {
    this.busy.set(true);
    this.api.put<void>(`/organizations/${this.organizationId()}`, { name: this.editName.trim(), description: this.editDescription.trim() || null }).subscribe({
      next: () => {
        this.organization.update(org => (org ? { ...org, name: this.editName.trim(), description: this.editDescription.trim() || null } : org));
        this.toasts.success('Organization updated.');
        this.busy.set(false);
      },
      error: error => {
        this.toasts.error(errorMessage(error));
        this.busy.set(false);
      },
    });
  }

  async deleteOrganization(org: Organization): Promise<void> {
    const confirmed = await this.confirm.ask({
      title: `Delete ${org.name}?`,
      message: 'This permanently deletes the organization, its teams, projects and issues. This cannot be undone.',
      confirmLabel: 'Delete organization',
      danger: true,
    });
    if (!confirmed) return;

    this.api.delete<void>(`/organizations/${org.id}`).subscribe({
      next: () => {
        this.toasts.success('Organization deleted.');
        void this.router.navigate(['/organizations']);
      },
      error: error => this.toasts.error(errorMessage(error)),
    });
  }

  private loadTeamMembers(team: Team): void {
    this.api.get<TeamMember[]>(`/teams/${team.id}/members`).subscribe(items => this.teamMembers.set(items));
  }
}
