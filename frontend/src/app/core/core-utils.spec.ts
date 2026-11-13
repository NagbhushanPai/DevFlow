import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Router, UrlTree, provideRouter } from '@angular/router';
import { describe, expect, it } from 'vitest';
import { authGuard, guestGuard } from './auth.guard';
import { AuthService } from './auth.service';
import { ConfirmService } from './confirm.service';
import { ISSUE_STATUSES, IssueStatus, ProjectRole, labelOf } from './models';
import { canManageOrganization, isOrganizationOwner, projectPermissions } from './permissions';
import { toFormErrors } from './problem';

describe('projectPermissions', () => {
  it('Viewers can only read', () => {
    const p = projectPermissions(ProjectRole.Viewer);
    expect(p.canContribute).toBe(false);
    expect(p.canManage).toBe(false);
    expect(p.canDeleteIssue('me', 'me')).toBe(false);
  });

  it('Developers contribute and delete only their own items', () => {
    const p = projectPermissions(ProjectRole.Developer);
    expect(p.canContribute).toBe(true);
    expect(p.canManage).toBe(false);
    expect(p.canDeleteIssue('me', 'me')).toBe(true);
    expect(p.canDeleteIssue('someone-else', 'me')).toBe(false);
  });

  it('Managers can do everything', () => {
    const p = projectPermissions(ProjectRole.Manager);
    expect(p.canManage).toBe(true);
    expect(p.canDeleteIssue('someone-else', 'me')).toBe(true);
    expect(p.canDeleteOwned('someone-else', 'me')).toBe(true);
  });

  it('non-members have no permissions', () => {
    expect(projectPermissions(null).canContribute).toBe(false);
  });

  it('organization roles', () => {
    expect(canManageOrganization('Owner')).toBe(true);
    expect(canManageOrganization('Admin')).toBe(true);
    expect(canManageOrganization('Member')).toBe(false);
    expect(isOrganizationOwner('Admin')).toBe(false);
  });
});

describe('toFormErrors', () => {
  const httpError = (status: number, error: unknown) => new HttpErrorResponse({ status, error });

  it('maps ValidationProblemDetails to camelCase field errors', () => {
    const result = toFormErrors(httpError(400, { title: 'Validation failed', errors: { Password: ['Too short'], 'Request.Email': ['Invalid'] } }));
    expect(result.fields).toEqual({ password: ['Too short'], email: ['Invalid'] });
    expect(result.message).toBe('Please fix the highlighted fields.');
  });

  it('joins registration error arrays into the message', () => {
    expect(toFormErrors(httpError(400, { detail: 'Registration failed', errors: ['Email taken.'] })).message).toBe('Email taken.');
  });

  it('uses the problem detail when present', () => {
    expect(toFormErrors(httpError(409, { title: 'Conflict', detail: 'Key exists.' })).message).toBe('Key exists.');
  });

  it('explains network failures and rate limiting', () => {
    expect(toFormErrors(httpError(0, null)).message).toContain('Cannot reach the server');
    expect(toFormErrors(httpError(429, null)).message).toContain('Too many requests');
  });

  it('falls back for non-HTTP errors', () => {
    expect(toFormErrors(new Error('boom'), 'Fallback').message).toBe('Fallback');
  });
});

describe('labelOf', () => {
  it('returns friendly labels', () => {
    expect(labelOf(ISSUE_STATUSES, IssueStatus.InProgress)).toBe('In progress');
    expect(labelOf(ISSUE_STATUSES, 99 as IssueStatus)).toBe('Unknown');
  });
});

describe('guards', () => {
  const setup = (authenticated: boolean) =>
    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: AuthService, useValue: { isAuthenticated: () => authenticated } }],
    });

  it('authGuard lets signed-in users through', () => {
    setup(true);
    expect(TestBed.runInInjectionContext(() => authGuard({} as never, { url: '/projects' } as never))).toBe(true);
  });

  it('authGuard redirects to login and remembers the target URL', () => {
    setup(false);
    const result = TestBed.runInInjectionContext(() => authGuard({} as never, { url: '/projects/42/board' } as never)) as UrlTree;
    expect(TestBed.inject(Router).serializeUrl(result)).toBe('/login?returnUrl=%2Fprojects%2F42%2Fboard');
  });

  it('guestGuard sends signed-in users to the dashboard', () => {
    setup(true);
    const result = TestBed.runInInjectionContext(() => guestGuard({} as never, {} as never)) as UrlTree;
    expect(TestBed.inject(Router).serializeUrl(result)).toBe('/dashboard');
  });
});

describe('ConfirmService', () => {
  it('resolves with the answer and supersedes older requests', async () => {
    const service = new ConfirmService();
    const first = service.ask({ title: 'A', message: 'a' });
    const second = service.ask({ title: 'B', message: 'b' });
    expect(service.pending()?.title).toBe('B');
    service.answer(true);

    expect(await first).toBe(false);
    expect(await second).toBe(true);
    expect(service.pending()).toBeNull();
  });
});
