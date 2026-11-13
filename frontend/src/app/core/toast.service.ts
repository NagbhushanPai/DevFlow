import { Injectable, signal } from '@angular/core';

export type ToastKind = 'success' | 'error' | 'info';

export interface Toast { id: number; kind: ToastKind; message: string; }

@Injectable({ providedIn: 'root' })
export class ToastService {
  private nextId = 1;
  private readonly items = signal<Toast[]>([]);
  readonly toasts = this.items.asReadonly();

  success(message: string): void { this.show('success', message); }
  error(message: string): void { this.show('error', message, 7000); }
  info(message: string): void { this.show('info', message); }

  dismiss(id: number): void {
    this.items.update(items => items.filter(toast => toast.id !== id));
  }

  private show(kind: ToastKind, message: string, durationMs = 4000): void {
    // Collapse duplicates (e.g. several parallel requests failing for the same reason).
    if (this.items().some(toast => toast.message === message)) {
      return;
    }

    const id = this.nextId++;
    this.items.update(items => [...items.slice(-3), { id, kind, message }]);
    setTimeout(() => this.dismiss(id), durationMs);
  }
}
