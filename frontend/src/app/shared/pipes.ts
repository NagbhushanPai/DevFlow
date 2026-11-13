import { Pipe, PipeTransform } from '@angular/core';
import { Option, labelOf } from '../core/models';

/** `{{ issue.status | label: statuses }}` → "In progress". */
@Pipe({ name: 'label', standalone: true })
export class LabelPipe implements PipeTransform {
  transform<T>(value: T | null | undefined, options: Option<T>[]): string {
    return labelOf(options, value);
  }
}

/** "just now", "5 min ago", "3 h ago", "2 d ago", then a short date. */
@Pipe({ name: 'relativeTime', standalone: true })
export class RelativeTimePipe implements PipeTransform {
  transform(value: string | Date | null | undefined, now: number = Date.now()): string {
    if (!value) {
      return '';
    }

    const time = new Date(value).getTime();
    if (Number.isNaN(time)) {
      return '';
    }

    const seconds = Math.round((now - time) / 1000);
    if (seconds < 45) return 'just now';
    const minutes = Math.round(seconds / 60);
    if (minutes < 60) return `${minutes} min ago`;
    const hours = Math.round(minutes / 60);
    if (hours < 24) return `${hours} h ago`;
    const days = Math.round(hours / 24);
    if (days < 7) return `${days} d ago`;
    return new Date(time).toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' });
  }
}

@Pipe({ name: 'fileSize', standalone: true })
export class FileSizePipe implements PipeTransform {
  transform(bytes: number | null | undefined): string {
    if (bytes === null || bytes === undefined || bytes < 0) return '';
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }
}

/** Formats a UTC ISO date as a local short date, or a fallback. */
@Pipe({ name: 'shortDate', standalone: true })
export class ShortDatePipe implements PipeTransform {
  transform(value: string | null | undefined, fallback = '—'): string {
    if (!value) return fallback;
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? fallback : date.toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' });
  }
}
