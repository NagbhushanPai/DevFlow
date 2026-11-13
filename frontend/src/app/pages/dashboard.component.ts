import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ApiService } from '../core/api.service';
import { AuthService } from '../core/auth.service';
import { Organization, PROJECT_ROLES, PROJECT_STATUSES, Page, Project, ProjectStatus } from '../core/models';
import { NotificationService } from '../core/notification.service';
import { LabelPipe, RelativeTimePipe } from '../shared/pipes';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, LabelPipe, RelativeTimePipe],
  template: `
    <section class="page">
      <header class="page-header">
        <div>
          <p class="eyebrow">Workspace overview</p>
          <h1>{{ greeting() }}, {{ auth.user()?.firstName }}</h1>
          <p>Here's what's happening across your projects.</p>
        </div>
        <a class="btn" routerLink="/projects">Browse projects</a>
      </header>

      @if (loading()) {
        <div class="grid">
          <div class="skeleton block"></div><div class="skeleton block"></div><div class="skeleton block"></div>
        </div>
      } @else {
        <div class="grid" style="margin-bottom: 24px">
          <div class="card stat"><strong>{{ activeProjects() }}</strong><span>Active projects</span></div>
          <div class="card stat"><strong>{{ organizations().length }}</strong><span>Organizations</span></div>
          <div class="card stat"><strong>{{ notifications.unreadCount() }}</strong><span>Unread notifications</span></div>
        </div>

        @if (organizations().length === 0) {
          <div class="card empty">
            <h3>Welcome to DevFlow 👋</h3>
            <p>Get started in three steps: create an organization, add a project, then create your first issue.</p>
            <a class="btn" routerLink="/organizations">Create an organization</a>
          </div>
        } @else {
          <div class="split">
            <div class="card">
              <div class="card-header">
                <h2>Your projects</h2>
                <a class="btn ghost sm" routerLink="/projects">View all</a>
              </div>
              @if (projects().length === 0) {
                <div class="empty">
                  <p>You're not a member of any project yet.</p>
                  <a class="btn secondary" routerLink="/projects">Create a project</a>
                </div>
              } @else {
                <ul class="list">
                  @for (project of projects(); track project.id) {
                    <li>
                      <span class="badge mono">{{ project.key }}</span>
                      <a [routerLink]="['/projects', project.id]" class="person-name"><strong>{{ project.name }}</strong></a>
                      <span class="spacer"></span>
                      <span class="badge hide-mobile">{{ project.status | label: projectStatuses }}</span>
                      @if (project.myRole) { <span class="badge role">{{ project.myRole | label: roles }}</span> }
                      <a class="btn ghost sm hide-mobile" [routerLink]="['/projects', project.id, 'board']">Board</a>
                    </li>
                  }
                </ul>
              }
            </div>

            <div class="card">
              <div class="card-header"><h2>Recent activity</h2></div>
              <ul class="list">
                @for (item of notifications.notifications().slice(0, 6); track item.id) {
                  <li style="flex-direction: column; align-items: flex-start; gap: 2px">
                    <strong style="font-size: .9rem">{{ item.title }}</strong>
                    <span class="muted" style="font-size: .85rem">{{ item.message }}</span>
                    <span class="subtle">{{ item.createdAtUtc | relativeTime }}</span>
                  </li>
                } @empty {
                  <li class="muted">No notifications yet. You'll be notified when issues are assigned to you or commented on.</li>
                }
              </ul>
            </div>
          </div>
        }
      }
    </section>
  `,
})
export class DashboardComponent {
  readonly auth = inject(AuthService);
  readonly notifications = inject(NotificationService);
  private readonly api = inject(ApiService);

  readonly projectStatuses = PROJECT_STATUSES;
  readonly roles = PROJECT_ROLES;
  readonly loading = signal(true);
  readonly projects = signal<Project[]>([]);
  readonly organizations = signal<Organization[]>([]);
  readonly activeProjects = computed(() => this.projects().filter(project => project.status === ProjectStatus.Active).length);
  readonly greeting = computed(() => {
    const hour = new Date().getHours();
    return hour < 12 ? 'Good morning' : hour < 18 ? 'Good afternoon' : 'Good evening';
  });

  constructor() {
    forkJoin({
      projects: this.api.get<Page<Project>>('/projects', { pageSize: 8, sortBy: 'name' }),
      organizations: this.api.get<Organization[]>('/organizations'),
    }).subscribe({
      next: ({ projects, organizations }) => {
        this.projects.set(projects.items);
        this.organizations.set(organizations);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
