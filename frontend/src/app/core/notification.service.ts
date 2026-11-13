import { Injectable, computed, inject, signal } from '@angular/core';
import { ApiService } from './api.service';
import { NotificationItem } from './models';
import { RealtimeService } from './realtime.service';
import { ToastService } from './toast.service';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly api = inject(ApiService);
  private readonly toasts = inject(ToastService);
  private readonly items = signal<NotificationItem[]>([]);

  readonly notifications = this.items.asReadonly();
  readonly unreadCount = computed(() => this.items().filter(item => !item.isRead).length);

  constructor() {
    inject(RealtimeService).notificationCreated$.subscribe(notification => {
      this.items.update(current => [notification, ...current.filter(item => item.id !== notification.id)].slice(0, 50));
      this.toasts.info(notification.title);
    });
  }

  load(): void {
    this.api.get<NotificationItem[]>('/notifications').subscribe({ next: items => this.items.set(items), error: () => undefined });
  }

  clear(): void {
    this.items.set([]);
  }

  markAsRead(id: string): void {
    const item = this.items().find(notification => notification.id === id);
    if (!item || item.isRead) {
      return;
    }

    this.setRead(notification => notification.id === id);
    this.api.post<void>(`/notifications/${id}/read`).subscribe({ error: () => this.load() });
  }

  markAllAsRead(): void {
    if (this.unreadCount() === 0) {
      return;
    }

    this.setRead(() => true);
    this.api.post<void>('/notifications/read-all').subscribe({ error: () => this.load() });
  }

  private setRead(predicate: (item: NotificationItem) => boolean): void {
    this.items.update(current => current.map(item => (predicate(item) ? { ...item, isRead: true } : item)));
  }
}
