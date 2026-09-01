import { describe, expect, it } from 'vitest';
import {
  formatCalendarDate,
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

// A pure calendar date (tournament start date, registration deadline, birth
// date) is submitted from an <input type="date"> as UTC midnight of the
// intended day ("2026-10-10" -> new Date() -> "2026-10-10T00:00:00.000Z").
// It has no real time-of-day, so — unlike a genuine instant — it must NOT be
// shifted into Argentina time on display: formatDateAr would roll UTC
// midnight back to 21:00 the previous day in Buenos Aires (UTC-3),
// displaying the wrong day for every viewer west of UTC.
describe('formatCalendarDate — pure date fields, no timezone shift', () => {
  it('renders UTC midnight as the same calendar day, not the previous one', () => {
    expect(formatCalendarDate('2026-10-10T00:00:00.000Z')).toBe('10/10/2026');
    // formatDateAr on the same value demonstrates the bug this guards against.
    expect(formatDateAr('2026-10-10T00:00:00.000Z')).toBe('09/10/2026');
  });

  it('returns a placeholder for empty or invalid input', () => {
    expect(formatCalendarDate('')).toBe('—');
    expect(formatCalendarDate(null)).toBe('—');
    expect(formatCalendarDate('not-a-date')).toBe('—');
  });
});
