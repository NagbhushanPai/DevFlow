import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth.service';
import { NotificationService } from './core/notification.service';
import { RealtimeService } from './core/realtime.service';

@Component({
  standalone: true,
  selector: 'app-root',
  imports: [CommonModule, RouterOutlet, RouterLink],
  template: `<header>
    <a routerLink="/dashboard" class="brand">DevFlow</a>
    <ng-container *ngIf="auth.isAuthenticated()">
      <div class="notifications">
        <button (click)="toggle()">Notifications <span *ngIf="notifications.unreadCount() > 0">({{ notifications.unreadCount() }})</span></button>
        <ul *ngIf="open()">
          <li *ngFor="let notification of notifications.notifications()" [class.unread]="!notification.isRead" (click)="notifications.markAsRead(notification.id)">
            <strong>{{ notification.title }}</strong>
            <p>{{ notification.message }}</p>
          </li>
          <li *ngIf="notifications.notifications().length === 0">No notifications</li>
        </ul>
      </div>
      <button (click)="signOut()">Sign out</button>
    </ng-container>
  </header>
  <main><router-outlet /></main>`
})
export class AppComponent {
  readonly auth = inject(AuthService);
  readonly notifications = inject(NotificationService);
  private readonly realtime = inject(RealtimeService);
  readonly open = signal(false);

  constructor() {
    if (this.auth.isAuthenticated()) {
      this.notifications.load();
      void this.realtime.connect();
    }
  }

  toggle() { this.open.update(value => !value); }
  signOut() { void this.realtime.stop(); this.auth.logout(); }
}
