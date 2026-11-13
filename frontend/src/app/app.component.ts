import { ChangeDetectionStrategy, Component, ElementRef, HostListener, effect, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { AuthService } from './core/auth.service';
import { NotificationService } from './core/notification.service';
import { RealtimeService } from './core/realtime.service';
import { AvatarComponent } from './shared/avatar.component';
import { ConfirmDialogComponent, ToastHostComponent } from './shared/overlays.component';
import { RelativeTimePipe } from './shared/pipes';

@Component({
  selector: 'app-root',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, AvatarComponent, ToastHostComponent, ConfirmDialogComponent, RelativeTimePipe],
  template: `
    <header class="app-header">
      @if (auth.isAuthenticated()) {
        <button class="btn ghost icon nav-toggle" type="button" (click)="navOpen.set(!navOpen())" aria-label="Toggle navigation">☰</button>
      }
      <a class="brand" [routerLink]="auth.isAuthenticated() ? '/dashboard' : '/login'">
        <span class="brand-mark" aria-hidden="true">DF</span> DevFlow
      </a>

      @if (auth.isAuthenticated()) {
        <nav class="main-nav" [class.open]="navOpen()" aria-label="Primary">
          <a routerLink="/dashboard" routerLinkActive="active">Dashboard</a>
          <a routerLink="/projects" routerLinkActive="active">Projects</a>
          <a routerLink="/organizations" routerLinkActive="active">Organizations</a>
        </nav>

        <div class="header-actions">
          <span class="connection-dot hide-mobile" [class]="'connection-dot hide-mobile ' + realtime.status()"
                [attr.title]="'Live updates: ' + realtime.status()"></span>

          <div class="popover-anchor">
            <button class="btn ghost" type="button" (click)="toggle('notifications')" [attr.aria-expanded]="openPanel() === 'notifications'"
                    aria-label="Notifications">
              🔔
              @if (notifications.unreadCount() > 0) {
                <span class="count-pill">{{ notifications.unreadCount() > 99 ? '99+' : notifications.unreadCount() }}</span>
              }
            </button>
            @if (openPanel() === 'notifications') {
              <div class="popover" role="dialog" aria-label="Notifications">
                <div class="popover-header">
                  <strong>Notifications</strong>
                  <button class="btn ghost sm" type="button" (click)="notifications.markAllAsRead()" [disabled]="notifications.unreadCount() === 0">
                    Mark all read
                  </button>
                </div>
                <ul class="popover-list">
                  @for (item of notifications.notifications(); track item.id) {
                    <li [class.unread]="!item.isRead" (click)="notifications.markAsRead(item.id)">
                      <strong>{{ item.title }}</strong>
                      <p>{{ item.message }}</p>
                      <span class="subtle">{{ item.createdAtUtc | relativeTime }}</span>
                    </li>
                  } @empty {
                    <li class="empty">You're all caught up.</li>
                  }
                </ul>
              </div>
            }
          </div>

          <div class="popover-anchor">
            <button class="btn ghost" type="button" (click)="toggle('user')" [attr.aria-expanded]="openPanel() === 'user'" aria-label="Account menu">
              <app-avatar [name]="auth.displayName()" [seed]="auth.user()?.userId" size="sm" />
              <span class="hide-mobile">{{ auth.user()?.firstName }}</span>
            </button>
            @if (openPanel() === 'user') {
              <div class="popover menu" role="menu">
                <div class="popover-header">
                  <div>
                    <strong>{{ auth.displayName() }}</strong>
                    <div class="subtle">{{ auth.user()?.email }}</div>
                  </div>
                </div>
                <button type="button" role="menuitem" (click)="signOut()">Sign out</button>
              </div>
            }
          </div>
        </div>
      }
    </header>

    <main>
      <router-outlet />
    </main>

    <app-toast-host />
    <app-confirm-dialog />
  `,
})
export class AppComponent {
  readonly auth = inject(AuthService);
  readonly notifications = inject(NotificationService);
  readonly realtime = inject(RealtimeService);
  private readonly host = inject(ElementRef<HTMLElement>);

  readonly openPanel = signal<'notifications' | 'user' | null>(null);
  readonly navOpen = signal(false);

  constructor() {
    // Start/stop per-user services whenever the session appears or disappears.
    effect(() => {
      if (this.auth.isAuthenticated()) {
        this.notifications.load();
        void this.realtime.connect();
      } else {
        this.notifications.clear();
        void this.realtime.stop();
        this.openPanel.set(null);
      }
    });

    inject(Router).events.pipe(filter(event => event instanceof NavigationEnd)).subscribe(() => {
      this.navOpen.set(false);
      this.openPanel.set(null);
    });
  }

  toggle(panel: 'notifications' | 'user'): void {
    this.openPanel.update(current => (current === panel ? null : panel));
  }

  signOut(): void {
    this.auth.logout();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as Node | null;
    if (this.openPanel() && target && !(target as Element).closest?.('.popover-anchor') && this.host.nativeElement.contains(target)) {
      this.openPanel.set(null);
    }
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.openPanel.set(null);
  }
}
