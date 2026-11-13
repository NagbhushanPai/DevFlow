import { Injectable, signal } from '@angular/core';

export interface ConfirmRequest {
  title: string;
  message: string;
  confirmLabel?: string;
  danger?: boolean;
}

interface PendingConfirm extends ConfirmRequest {
  resolve: (confirmed: boolean) => void;
}

/** Promise-based confirmation dialog rendered by ConfirmDialogComponent in the app shell. */
@Injectable({ providedIn: 'root' })
export class ConfirmService {
  private readonly pendingRequest = signal<PendingConfirm | null>(null);
  readonly pending = this.pendingRequest.asReadonly();

  ask(request: ConfirmRequest): Promise<boolean> {
    this.pendingRequest()?.resolve(false);
    return new Promise(resolve => this.pendingRequest.set({ ...request, resolve }));
  }

  answer(confirmed: boolean): void {
    const request = this.pendingRequest();
    this.pendingRequest.set(null);
    request?.resolve(confirmed);
  }
}
