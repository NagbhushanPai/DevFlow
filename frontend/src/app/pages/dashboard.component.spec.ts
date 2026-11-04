import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { provideRouter } from '@angular/router';
import { describe, expect, it } from 'vitest';
import { AuthService } from '../core/auth.service';
import { DashboardComponent } from './dashboard.component';

describe('DashboardComponent', () => {
  it('renders the authenticated user name and navigation cards', async () => {
    const fixture: ComponentFixture<DashboardComponent> = await TestBed.configureTestingModule({
      imports: [DashboardComponent],
      providers: [
        provideRouter([]),
        {
          provide: AuthService,
          useValue: {
            user: signal({ firstName: 'Test', lastName: 'User' })
          }
        }
      ]
    }).createComponent(DashboardComponent);

    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Welcome, Test.');
    expect(fixture.nativeElement.querySelectorAll('a.nav-card')).toHaveLength(2);
  });
});
