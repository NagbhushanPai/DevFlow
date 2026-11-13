import { ChangeDetectionStrategy, Component, OnDestroy, effect, inject, input } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { PROJECT_ROLES, PROJECT_STATUSES } from '../../core/models';
import { RealtimeService } from '../../core/realtime.service';
import { LabelPipe } from '../../shared/pipes';
import { ProjectContext } from './project-context';

/** Header + tabs for one project. Provides ProjectContext to the tab components and joins the realtime group. */
@Component({
  selector: 'app-project-shell',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, LabelPipe],
  providers: [ProjectContext],
  template: `
    <section class="page">
      @if (context.project(); as project) {
        <header class="page-header" style="margin-bottom: 16px">
          <div>
            <nav class="breadcrumbs"><a routerLink="/projects">Projects</a> / <span class="mono">{{ project.key }}</span></nav>
            <h1>{{ project.name }}</h1>
            @if (project.description) { <p>{{ project.description }}</p> }
          </div>
          <div class="row">
            <span class="badge">{{ project.status | label: statuses }}</span>
            @if (project.myRole) { <span class="badge role">{{ project.myRole | label: roles }}</span> }
          </div>
        </header>

        <nav class="tabs" aria-label="Project sections">
          <a routerLink="issues" routerLinkActive="active">Issues</a>
          <a routerLink="board" routerLinkActive="active">Board</a>
          <a routerLink="sprints" routerLinkActive="active">Sprints</a>
          <a routerLink="settings" routerLinkActive="active">{{ context.permissions().canManage ? 'Settings' : 'Members & info' }}</a>
        </nav>

        <router-outlet />
      } @else if (context.error()) {
        <div class="card empty">
          <h3>Project unavailable</h3>
          <p>{{ context.error() }}</p>
          <a class="btn" routerLink="/projects">Back to projects</a>
        </div>
      } @else {
        <div class="skeleton" style="width: 35%; height: 28px; margin-bottom: 20px"></div>
        <div class="skeleton block"></div>
      }
    </section>
  `,
})
export class ProjectShellComponent implements OnDestroy {
  readonly context = inject(ProjectContext);
  private readonly realtime = inject(RealtimeService);
  private joined?: string;

  readonly projectId = input.required<string>();
  readonly statuses = PROJECT_STATUSES;
  readonly roles = PROJECT_ROLES;

  constructor() {
    effect(() => {
      const id = this.projectId();
      this.context.load(id);
      if (this.joined && this.joined !== id) void this.realtime.leaveProject(this.joined);
      this.joined = id;
      void this.realtime.joinProject(id);
    });
  }

  ngOnDestroy(): void {
    if (this.joined) void this.realtime.leaveProject(this.joined);
  }
}
