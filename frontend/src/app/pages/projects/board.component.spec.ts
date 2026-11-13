import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { computed, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Subject } from 'rxjs';
import { beforeEach, describe, expect, it } from 'vitest';
import { Issue, IssuePriority, IssueStatus, IssueType, Project, ProjectRole, ProjectStatus } from '../../core/models';
import { projectPermissions } from '../../core/permissions';
import { RealtimeService } from '../../core/realtime.service';
import { BoardComponent } from './board.component';
import { ProjectContext } from './project-context';

const project: Project = {
  id: 'p1', organizationId: 'o1', name: 'Web', key: 'WEB', status: ProjectStatus.Active, ownerId: 'u1', createdAtUtc: '2026-01-01T00:00:00Z', myRole: ProjectRole.Developer,
};

const issue = (overrides: Partial<Issue>): Issue => ({
  id: 'i1', projectId: 'p1', issueNumber: 1, title: 'Fix login', type: IssueType.Bug, status: IssueStatus.Todo,
  priority: IssuePriority.High, reporterId: 'u1', createdAtUtc: '2026-01-01T00:00:00Z', ...overrides,
});

describe('BoardComponent', () => {
  let http: HttpTestingController;
  let role: ReturnType<typeof signal<ProjectRole>>;

  beforeEach(() => {
    role = signal(ProjectRole.Developer);
    const context = {
      project: signal(project),
      members: signal([]),
      permissions: computed(() => projectPermissions(role())),
      userId: signal('u1'),
      nameOf: () => '',
    };
    const realtime = { issueCreated$: new Subject(), issueUpdated$: new Subject(), issueDeleted$: new Subject() };

    TestBed.configureTestingModule({
      imports: [BoardComponent],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ProjectContext, useValue: context },
        { provide: RealtimeService, useValue: realtime },
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });

  const render = (issues: Issue[]) => {
    const fixture = TestBed.createComponent(BoardComponent);
    http.expectOne(req => req.url === '/api/projects/p1/issues').flush({ items: issues, page: 1, pageSize: 100, totalCount: issues.length });
    http.expectOne('/api/projects/p1/sprints').flush([]);
    fixture.detectChanges();
    return fixture;
  };

  it('groups issues into status columns', () => {
    const fixture = render([issue({ id: 'a', status: IssueStatus.Todo }), issue({ id: 'b', status: IssueStatus.Done, issueNumber: 2 })]);
    const component = fixture.componentInstance;

    expect(component.byStatus().get(IssueStatus.Todo)?.map(i => i.id)).toEqual(['a']);
    expect(component.byStatus().get(IssueStatus.Done)?.map(i => i.id)).toEqual(['b']);
    expect(fixture.nativeElement.textContent).toContain('WEB-1');
  });

  it('moves a card optimistically and sends the full update', () => {
    const fixture = render([issue({})]);
    const component = fixture.componentInstance;

    component.move(component.issues()[0], IssueStatus.InProgress);
    expect(component.issues()[0].status).toBe(IssueStatus.InProgress);

    const request = http.expectOne('/api/issues/i1');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toMatchObject({ title: 'Fix login', status: IssueStatus.InProgress, priority: IssuePriority.High });
    request.flush(null);
  });

  it('rolls back the move if the API rejects it', () => {
    const fixture = render([issue({})]);
    const component = fixture.componentInstance;

    component.move(component.issues()[0], IssueStatus.Done);
    http.expectOne('/api/issues/i1').flush({ detail: 'nope' }, { status: 403, statusText: 'Forbidden' });

    expect(component.issues()[0].status).toBe(IssueStatus.Todo);
  });

  it('is read-only for viewers', () => {
    role.set(ProjectRole.Viewer);
    const fixture = render([issue({})]);

    expect(fixture.componentInstance.canMove()).toBe(false);
    expect(fixture.nativeElement.textContent).toContain("viewers can't move cards");
    expect(fixture.nativeElement.querySelector('.issue-card').getAttribute('draggable')).toBe('false');
  });
});
