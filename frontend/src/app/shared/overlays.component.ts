import { ChangeDetectionStrategy, Component, ElementRef, HostListener, effect, inject, viewChild } from '@angular/core';
import { ConfirmService } from '../core/confirm.service';
import { ToastService } from '../core/toast.service';

@Component({
  selector: 'app-toast-host',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="toasts" role="status" aria-live="polite">
      @for (toast of toasts.toasts(); track toast.id) {
        <div class="toast" [class.success]="toast.kind === 'success'" [class.error]="toast.kind === 'error'">
          <p>{{ toast.message }}</p>
          <button class="btn ghost icon sm" type="button" (click)="toasts.dismiss(toast.id)" aria-label="Dismiss">✕</button>
        </div>
      }
    </div>
  `,
})
export class ToastHostComponent {
  readonly toasts = inject(ToastService);
}

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (confirm.pending(); as request) {
      <div class="backdrop" (click)="confirm.answer(false)">
        <div class="dialog sm" role="alertdialog" aria-modal="true" [attr.aria-label]="request.title" (click)="$event.stopPropagation()">
          <h2>{{ request.title }}</h2>
          <p class="muted">{{ request.message }}</p>
          <div class="form-actions">
            <button class="btn secondary" type="button" (click)="confirm.answer(false)">Cancel</button>
            <button #confirmButton class="btn" [class.danger]="request.danger" type="button" (click)="confirm.answer(true)">
              {{ request.confirmLabel || 'Confirm' }}
            </button>
          </div>
        </div>
      </div>
    }
  `,
})
export class ConfirmDialogComponent {
  readonly confirm = inject(ConfirmService);
  private readonly confirmButton = viewChild<ElementRef<HTMLButtonElement>>('confirmButton');

  constructor() {
    effect(() => this.confirmButton()?.nativeElement.focus());
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.confirm.pending()) {
      this.confirm.answer(false);
    }
  }
}
