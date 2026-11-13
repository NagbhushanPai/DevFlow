import { Injectable, computed, inject, signal } from '@angular/core';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { Project, ProjectMember } from '../../core/models';
import { projectPermissions } from '../../core/permissions';

/**
 * State shared by every tab of one project (provided by ProjectShellComponent): the project, its members and
 * what the current user may do in it.
 */
@Injectable()
export class ProjectContext {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);

  readonly project = signal<Project | null>(null);
  readonly members = signal<ProjectMember[]>([]);
  readonly error = signal('');
  readonly permissions = computed(() => projectPermissions(this.project()?.myRole));
  readonly userId = computed(() => this.auth.user()?.userId);
  readonly memberNames = computed(() => new Map(this.members().map(member => [member.userId, member.fullName])));

  load(projectId: string): void {
    this.error.set('');
    this.api.get<Project>(`/projects/${projectId}`).subscribe({
      next: project => this.project.set(project),
      error: (error: { status?: number }) =>
        this.error.set(error.status === 403 ? 'You are not a member of this project.' : error.status === 404 ? 'This project does not exist.' : 'The project could not be loaded.'),
    });
    this.loadMembers(projectId);
  }

  loadMembers(projectId = this.project()?.id): void {
    if (!projectId) return;
    this.api.get<ProjectMember[]>(`/projects/${projectId}/members`).subscribe({ next: members => this.members.set(members), error: () => undefined });
  }

  nameOf(userId: string | null | undefined): string {
    return userId ? (this.memberNames().get(userId) ?? 'Former member') : '';
  }
}
