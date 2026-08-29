/**
 * Shared email / phone validators reused by every form that collects contact
 * data (user create/invite/edit, player create/edit, password recovery).
 *
 * The rules intentionally mirror the backend DataAnnotations in
 * `Application.Utils.Constants.Validation.ValidationPatterns`, so the client and
 * the server accept exactly the same values and the user never gets a 400 for
 * input the form said was fine.
 */

/** Standard, permissive email shape: local@domain.tld with no whitespace. */
const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/** Characters allowed in a plausible phone number. */
const PHONE_ALLOWED_CHARS_REGEX = /^[+\d\s()-]+$/;

const PHONE_MIN_DIGITS = 8;
const PHONE_MAX_DIGITS = 15;

/** True when `value` is a syntactically valid email address. */
export function isValidEmail(value: string): boolean {
  return EMAIL_REGEX.test(value.trim());
}

/**
 * True when `value` is a plausible phone number: only digits, spaces, `+`,
 * `-`, and parentheses, containing between 8 and 15 digits.
 */
export function isValidPhone(value: string): boolean {
  const trimmed = value.trim();

  if (!PHONE_ALLOWED_CHARS_REGEX.test(trimmed)) {
    return false;
  }

  const digitCount = (trimmed.match(/\d/g) ?? []).length;
  return digitCount >= PHONE_MIN_DIGITS && digitCount <= PHONE_MAX_DIGITS;
}

/** Spanish (voseo) helper/error messages shown under the fields. */
export const VALIDATION_MESSAGES = {
  email: 'Ingresá un email válido',
  phone: 'Ingresá un teléfono válido',
} as const;
