import { describe, expect, it } from 'vitest';
import { isValidEmail, isValidPhone } from './validators';

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
