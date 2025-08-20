import dayjs from 'dayjs';
import utc from 'dayjs/plugin/utc';
import timezone from 'dayjs/plugin/timezone';
import 'dayjs/locale/es';

dayjs.extend(utc);
dayjs.extend(timezone);

/**
 * Converts a UTC date string to a formatted local date string in Spanish.
 * @param dateString - The date string in UTC format.
 * @returns The formatted date in local timezone, e.g. "lunes, 16 de agosto de 2026 • 14:30".
 */
export function formatMatchDateToString(dateString: string): string {
  const localDate = dayjs.utc(dateString).local();
  return localDate.locale('es').format('dddd, D [de] MMMM [de] YYYY • HH:mm');
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
  // Creates a new Date object for the current time
  const now = new Date();

  // Compares whether the deadline date is greater than the current date
  // The comparison is made in milliseconds since the Unix epoch.
  return new Date(deadline) > now;
};
