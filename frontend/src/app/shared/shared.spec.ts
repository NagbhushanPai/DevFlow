import { describe, expect, it } from 'vitest';
import { PASSWORD_RULES } from '../pages/auth/register.component';
import { safeReturnUrl } from '../pages/auth/login.component';
import { colorFor, initials } from './avatar.component';
import { FileSizePipe, RelativeTimePipe, ShortDatePipe } from './pipes';

describe('pipes', () => {
  const now = new Date('2026-06-01T12:00:00Z').getTime();
  const relative = new RelativeTimePipe();

  it('relativeTime', () => {
    expect(relative.transform('2026-06-01T11:59:50Z', now)).toBe('just now');
    expect(relative.transform('2026-06-01T11:55:00Z', now)).toBe('5 min ago');
    expect(relative.transform('2026-06-01T09:00:00Z', now)).toBe('3 h ago');
    expect(relative.transform('2026-05-30T12:00:00Z', now)).toBe('2 d ago');
    expect(relative.transform(null)).toBe('');
    expect(relative.transform('not a date')).toBe('');
  });

  it('fileSize', () => {
    const pipe = new FileSizePipe();
    expect(pipe.transform(512)).toBe('512 B');
    expect(pipe.transform(2048)).toBe('2.0 KB');
    expect(pipe.transform(5 * 1024 * 1024)).toBe('5.0 MB');
  });

  it('shortDate falls back for empty values', () => {
    expect(new ShortDatePipe().transform(null, 'None')).toBe('None');
  });
});

describe('avatar helpers', () => {
  it('initials', () => {
    expect(initials('Ada Lovelace')).toBe('AL');
    expect(initials('Cher')).toBe('C');
    expect(initials('  ')).toBe('?');
  });

  it('colorFor is stable', () => {
    expect(colorFor('user-1')).toBe(colorFor('user-1'));
  });
});

describe('safeReturnUrl', () => {
  it('only allows in-app paths', () => {
    expect(safeReturnUrl('/projects/1')).toBe('/projects/1');
    expect(safeReturnUrl('https://evil.example')).toBe('/dashboard');
    expect(safeReturnUrl('//evil.example')).toBe('/dashboard');
    expect(safeReturnUrl('/login')).toBe('/dashboard');
    expect(safeReturnUrl(undefined)).toBe('/dashboard');
  });
});

describe('PASSWORD_RULES', () => {
  const passes = (value: string) => PASSWORD_RULES.every(rule => rule.test(value));

  it('mirror the API password policy', () => {
    expect(passes('Developer@123')).toBe(true);
    expect(passes('password')).toBe(false);
    expect(passes('Password1')).toBe(false);
    expect(passes('Pa@1')).toBe(false);
  });
});
