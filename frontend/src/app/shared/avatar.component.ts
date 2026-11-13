import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

const PALETTE = ['#4c6ef5', '#15aabf', '#12b886', '#fab005', '#fd7e14', '#e64980', '#7950f2', '#228be6', '#40c057', '#be4bdb'];

export function initials(name: string | null | undefined): string {
  const parts = (name ?? '').trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) return '?';
  return (parts[0][0] + (parts.length > 1 ? parts[parts.length - 1][0] : '')).toUpperCase();
}

export function colorFor(seed: string | null | undefined): string {
  let hash = 0;
  for (const char of seed ?? '') {
    hash = (hash * 31 + char.charCodeAt(0)) | 0;
  }
  return PALETTE[Math.abs(hash) % PALETTE.length];
}

/** Initials avatar with a stable colour per user. Optionally shows the name next to it. */
@Component({
  selector: 'app-avatar',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span class="person" [attr.title]="name() || null">
      <span class="avatar" [class.sm]="size() === 'sm'" [class.lg]="size() === 'lg'" [style.background]="color()" aria-hidden="true">{{ letters() }}</span>
      @if (showName()) {
        <span class="person-name">{{ name() || placeholder() }}</span>
      }
    </span>
  `,
})
export class AvatarComponent {
  readonly name = input<string | null | undefined>('');
  readonly seed = input<string | null | undefined>(undefined);
  readonly size = input<'sm' | 'md' | 'lg'>('md');
  readonly showName = input(false);
  readonly placeholder = input('Unassigned');

  readonly letters = computed(() => (this.name() ? initials(this.name()) : '–'));
  readonly color = computed(() => (this.name() ? colorFor(this.seed() ?? this.name()) : 'var(--border-strong)'));
}
