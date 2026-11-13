import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { computed, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { describe, expect, it } from 'vitest';
import { AuthService } from '../core/auth.service';
import { ProjectRole, ProjectStatus } from '../core/models';
import { NotificationService } from '../core/notification.service';
import { DashboardComponent } from './dashboard.component';

describe('DashboardComponent', () => {
  const setup = () => {
    TestBed.configureTestingModule({
      imports: [DashboardComponent],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: { user: signal({ firstName: 'Ada' }) } },
        { provide: NotificationService, useValue: { notifications: signal([]), unreadCount: computed(() => 3) } },
      ],
    });
    const fixture = TestBed.createComponent(DashboardComponent);
    return { fixture, http: TestBed.inject(HttpTestingController) };
  };

  it('greets the user and lists their projects with their role', () => {
    const { fixture, http } = setup();
    http.expectOne(req => req.url === '/api/projects').flush({
      items: [{ id: 'p1', organizationId: 'o1', name: 'DevFlow Web', key: 'WEB', status: ProjectStatus.Active, myRole: ProjectRole.Manager }],
      page: 1, pageSize: 8, totalCount: 1,
    });
    http.expectOne('/api/organizations').flush([{ id: 'o1', name: 'Acme', ownerId: 'u1', myRole: 'Owner' }]);
    fixture.detectChanges();

    const text: string = fixture.nativeElement.textContent;
    expect(text).toContain('Ada');
    expect(text).toContain('DevFlow Web');
    expect(text).toContain('Manager');
    expect(text).toContain('3');
  });

  it('shows onboarding when the user has no organizations', () => {
    const { fixture, http } = setup();
    http.expectOne(req => req.url === '/api/projects').flush({ items: [], page: 1, pageSize: 8, totalCount: 0 });
    http.expectOne('/api/organizations').flush([]);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Welcome to DevFlow');
    expect(fixture.nativeElement.querySelector('a[href="/organizations"]')).not.toBeNull();
  });
});
