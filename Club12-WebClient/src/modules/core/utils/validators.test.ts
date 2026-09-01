import { describe, expect, it } from 'vitest';
import {
  formatDocumentNumber,
  isAtLeastMinimumPlayerAge,
  isValidDocumentNumber,
  isValidEmail,
  isValidPhone,
} from './validators';

describe('isValidEmail', () => {
  it.each([
    'user@example.com',
    'jugador.12@club.com.ar',
    'a@b.co',
    '  spaced@example.com  ',
  ])('accepts a valid email: %s', email => {
    expect(isValidEmail(email)).toBe(true);
  });

  it.each([
    '',
    'plainaddress',
    'missing@domain',
    '@no-local.com',
    'no-at-sign.com',
    'spaces in@email.com',
    'double@@example.com',
  ])('rejects an invalid email: %s', email => {
    expect(isValidEmail(email)).toBe(false);
  });
});

describe('isValidPhone', () => {
  it.each([
    '1123456789',
    '+54 11 2345-6789',
    '(011) 4567-8901',
    '11 2345 6789',
    '+54-9-11-2345-6789',
  ])('accepts a plausible phone: %s', phone => {
    expect(isValidPhone(phone)).toBe(true);
  });

  it.each([
    '',
    '123', // too few digits
    '1234567', // 7 digits, below minimum
    '1234567890123456', // 16 digits, above maximum
    'abc12345678', // letters not allowed
    '11 2345 6789 ext.4', // letters not allowed
  ])('rejects an invalid phone: %s', phone => {
    expect(isValidPhone(phone)).toBe(false);
  });

  it('counts only digits toward the 8-15 range, ignoring separators', () => {
    expect(isValidPhone('(11) 1234-5678')).toBe(true);
    expect(isValidPhone('1-2-3-4-5-6-7')).toBe(false);
  });
});

describe('isValidDocumentNumber', () => {
  it.each(['30111222', '1234567', '999999999999999'])(
    'accepts a digits-only document number: %s',
    value => {
      expect(isValidDocumentNumber(value)).toBe(true);
    }
  );

  it.each([
    '', // empty
    'd23', // letters
    '12345', // too short (5 digits)
    '1234567890123456', // too long (16 digits)
    '30.111.222', // formatted with dots — reject, not accept-and-strip
  ])('rejects a non-digits-only document number: %s', value => {
    expect(isValidDocumentNumber(value)).toBe(false);
  });
});

describe('formatDocumentNumber', () => {
  it('adds dot thousands-separators to a valid document number', () => {
    expect(formatDocumentNumber('38742615')).toBe('38.742.615');
    expect(formatDocumentNumber('412281692')).toBe('412.281.692');
  });

  it('returns non-numeric input unchanged instead of mangling it', () => {
    expect(formatDocumentNumber('d23')).toBe('d23');
  });
});

describe('isAtLeastMinimumPlayerAge', () => {
  // Builds "YYYY-MM-DD" from LOCAL date parts (not toISOString, which
  // converts to UTC and can shift the calendar date near local midnight
  // in timezones behind UTC, e.g. Argentina).
  const isoDateYearsAgo = (years: number): string => {
    const date = new Date();
    date.setFullYear(date.getFullYear() - years);
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  };

  it('accepts someone older than the minimum age', () => {
    expect(isAtLeastMinimumPlayerAge(isoDateYearsAgo(20))).toBe(true);
  });

  it('accepts someone exactly at the minimum age', () => {
    expect(isAtLeastMinimumPlayerAge(isoDateYearsAgo(15))).toBe(true);
  });

  it('rejects someone younger than the minimum age', () => {
    expect(isAtLeastMinimumPlayerAge(isoDateYearsAgo(10))).toBe(false);
  });

  it('rejects an unparsable date', () => {
    expect(isAtLeastMinimumPlayerAge('not-a-date')).toBe(false);
  });
});
