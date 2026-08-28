import { describe, expect, it } from 'vitest';
import {
  formatDateAr,
  formatDateTimeAr,
  formatTimeAr,
  formatLongDateTimeAr,
  toArDayKey,
  formatArDayLabel,
} from './formatDate';

// Argentina (America/Argentina/Buenos_Aires) is UTC-3 year-round, so a UTC
// instant always renders three hours earlier. These assertions must hold
// regardless of the machine timezone the test runs on (HU-100).
describe('Argentina-time formatting helpers', () => {
  it('formats a known UTC instant as AR date + time', () => {
    expect(formatDateTimeAr('2026-08-16T17:30:00Z')).toBe('16/08/2026 14:30');
  });

  it('formats the date portion in AR time', () => {
    expect(formatDateAr('2026-08-16T17:30:00Z')).toBe('16/08/2026');
  });

  it('formats the time portion in AR time', () => {
    expect(formatTimeAr('2026-08-16T17:30:00Z')).toBe('14:30');
  });

  it('rolls back to the previous AR day for an early-UTC instant', () => {
    // 02:00 UTC on Jan 1 is 23:00 the previous day in Buenos Aires.
    expect(formatDateAr('2026-01-01T02:00:00Z')).toBe('31/12/2025');
    expect(formatTimeAr('2026-01-01T02:00:00Z')).toBe('23:00');
    expect(toArDayKey('2026-01-01T02:00:00Z')).toBe('2025-12-31');
  });

  it('produces a long Spanish AR date-time label', () => {
    expect(formatLongDateTimeAr('2026-08-16T17:30:00Z')).toContain(
      '16 de agosto de 2026 • 14:30'
    );
  });

  it('labels a day key with a capitalized Spanish weekday', () => {
    expect(formatArDayLabel('2026-08-16')).toContain('16 de agosto');
    expect(formatArDayLabel('2026-08-16')[0]).toBe(
      formatArDayLabel('2026-08-16')[0].toUpperCase()
    );
  });

  it('returns placeholders for empty or invalid input', () => {
    expect(formatDateAr('')).toBe('—');
    expect(formatDateTimeAr(null)).toBe('—');
    expect(formatDateAr('not-a-date')).toBe('—');
    expect(toArDayKey('')).toBe('unknown');
  });
});
