import dayjs, { Dayjs } from 'dayjs';
import utc from 'dayjs/plugin/utc';
import timezone from 'dayjs/plugin/timezone';
import 'dayjs/locale/es';

dayjs.extend(utc);
dayjs.extend(timezone);

/**
 * Canonical display timezone for the whole app (HU-100). The backend stores
 * and returns instants in UTC; every user-facing date/time is presented in
 * Argentina time regardless of the viewer's own timezone.
 */
export const AR_TIMEZONE = 'America/Argentina/Buenos_Aires';

/** Parses a UTC value and shifts it to Argentina time. */
const toArDayjs = (value: Date | string): Dayjs =>
  dayjs.utc(value).tz(AR_TIMEZONE);

/**
 * Formats a UTC value as a short Argentina-time date, e.g. "16/08/2026".
 * Returns "—" for empty or unparseable input.
 */
export function formatDateAr(value?: Date | string | null): string {
  if (!value) return '—';
  const parsed = toArDayjs(value);
  return parsed.isValid() ? parsed.format('DD/MM/YYYY') : '—';
}

/**
 * Formats a UTC value as a short Argentina-time date and time, e.g.
 * "16/08/2026 14:30". Returns "—" for empty or unparseable input.
 */
export function formatDateTimeAr(value?: Date | string | null): string {
  if (!value) return '—';
  const parsed = toArDayjs(value);
  return parsed.isValid() ? parsed.format('DD/MM/YYYY HH:mm') : '—';
}

/**
 * Formats a UTC value as an Argentina-time clock time, e.g. "14:30".
 * Returns "—" for empty or unparseable input.
 */
export function formatTimeAr(value?: Date | string | null): string {
  if (!value) return '—';
  const parsed = toArDayjs(value);
  return parsed.isValid() ? parsed.format('HH:mm') : '—';
}

/**
 * Formats a UTC value as a long Spanish Argentina-time date and time, e.g.
 * "lunes, 16 de agosto de 2026 • 14:30". Returns "—" for empty/invalid input.
 */
export function formatLongDateTimeAr(value?: Date | string | null): string {
  if (!value) return '—';
  const parsed = toArDayjs(value);
  return parsed.isValid()
    ? parsed.locale('es').format('dddd, D [de] MMMM [de] YYYY • HH:mm')
    : '—';
}

/**
 * Argentina-time calendar-day key ("YYYY-MM-DD") for a UTC value, or
 * "unknown" for empty/invalid input. Used to group items (e.g. fixtures) by
 * their Argentina-time day so an instant near midnight lands on the day the
 * user actually sees, not the UTC day.
 */
export function toArDayKey(value?: Date | string | null): string {
  if (!value) return 'unknown';
  const parsed = toArDayjs(value);
  return parsed.isValid() ? parsed.format('YYYY-MM-DD') : 'unknown';
}

/**
 * Formats a "YYYY-MM-DD" day key (see toArDayKey) as a long, capitalized
 * Spanish weekday + day + month label, e.g. "Jueves, 1 de enero".
 */
export function formatArDayLabel(dayKey: string): string {
  const parsed = dayjs(dayKey);
  if (!parsed.isValid()) return 'Fecha a confirmar';
  const label = parsed.locale('es').format('dddd, D [de] MMMM');
  return label.charAt(0).toUpperCase() + label.slice(1);
}

/**
 * Converts a UTC date string to a formatted Argentina-time date string in
 * Spanish, e.g. "lunes, 16 de agosto de 2026 • 14:30".
 * @param dateString - The date string in UTC format.
 */
export function formatMatchDateToString(dateString: string): string {
  return formatLongDateTimeAr(dateString);
}

/**
 * Converts a UTC date string to a local Date object.
 * @param dateString - The date string in UTC format.
 * @returns A JavaScript Date object in the local timezone.
 */
export function parseUTCToLocalDate(dateString: string): Date {
  if (!dateString) return new Date(NaN);

  return dayjs.utc(dateString).local().toDate();
}

/**
 * Converts a Date object (assumed UTC) to a local Date object.
 * @param date - The Date object in UTC.
 * @returns A JavaScript Date object in the local timezone.
 */
export function convertToLocalDate(date: Date): Date {
  if (!date) return new Date(NaN);

  return dayjs.utc(date).local().toDate();
}

/**
 * Converts a Date to a string compatible with <input type="datetime-local"> in local time.
 * @param date - The Date object (UTC or local)
 * @returns string in "YYYY-MM-DDTHH:mm" format
 */
export function formatDateTimeInput(date: Date): string {
  if (!date) return '';
  return dayjs(date).local().format('YYYY-MM-DDTHH:mm');
}

/**
 * Compares whether a deadline date is after the current date.
 * @param {Date} deadline - The deadline to compare.
 * @returns {boolean} - 'true' if the deadline has not yet passed, 'false' if it has already passed.
 */
export const isDeadlineInTheFuture = (deadline: Date): boolean => {
  const now = new Date();
  return new Date(deadline) > now;
};
