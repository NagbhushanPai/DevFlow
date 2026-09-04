import { Injectable, computed, inject, signal } from '@angular/core';
import { ApiService } from './api.service';
import { RealtimeService } from './realtime.service';

export interface NotificationItem { id: string; title: string; message: string; isRead: boolean; createdAtUtc: string; }

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly api = inject(ApiService);
  private readonly realtime = inject(RealtimeService);
  private readonly items = signal<NotificationItem[]>([]);
  readonly notifications = this.items.asReadonly();
  readonly unreadCount = computed(() => this.items().filter(n => !n.isRead).length);

  constructor() {
    this.realtime.notificationCreated$.subscribe(notification => this.items.update(current => [notification, ...current]));
  }

  load() { this.api.get<NotificationItem[]>('/notifications').subscribe(items => this.items.set(items)); }

  markAsRead(id: string) {
    this.api.post<void>(`/notifications/${id}/read`, {}).subscribe(() =>
      this.items.update(current => current.map(n => n.id === id ? { ...n, isRead: true } : n)));
  }
}
